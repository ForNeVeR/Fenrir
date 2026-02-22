// SPDX-FileCopyrightText: 2020-2026 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.ArgumentCommands

open System
open System.IO

open System.Threading.Tasks
open FSharp.Control
open Fenrir.Git
open Fenrir.Git.Metadata
open JetBrains.Lifetimes
open TruePath

module ExitCodes =
    let Success = 0
    let UnrecognizedArguments = 1

let private printUnrecognizedArguments argv =
    printfn $"Arguments were not recognized: %A{argv}."

let unrecognizedArgs(argv: string[]): int =
    printUnrecognizedArguments argv
    ExitCodes.UnrecognizedArguments

let PrintAllRefs(path: LocalPath): unit =
    Refs.ReadRefs path
    |> Seq.iter(fun ref -> printfn $"%s{ref.Name}: {ref.CommitObjectId}")

let PrintAllCommits(gitDir: LocalPath): Task<int> =
    task {
        let! head = Refs.ReadHead gitDir
        match head with
        | null -> ()
        | head ->
            do! (
                Commits.TraverseCommits(gitDir, head.CommitObjectId)
                |> AsyncSeq.iter (fun commit ->
                    // TODO[#88]: Properly gather commit messages
                    let message = Array.tryHead commit.Body.Rest |> Option.defaultValue ""
                    printfn $"{commit.Hash} {message}"
                )
            )
        return ExitCodes.Success
    }

let updateCommitOp (commitHash: Sha1Hash)
                   (pathToRepo: LocalPath)
                   (filePath: string)
                   (detachedAllowed: bool): Task<int> = task {
    let pathToDotGit = pathToRepo / ".git"
    if not detachedAllowed && Refs.IsHeadDetached pathToDotGit then
        printfn "You are in the detached head mode. Any repository modifications may turn it FUBAR.
If you are ready to spend a fair chunk of your time on Stack Overflow or aware of what you're doing, provide the --force key.
If at any moment your repository has turned FUBAR, consider revising the results of 'git log --reflog' to locate any commits missing."
    else
        let fullPathToFile = pathToRepo / filePath
        use ld = new LifetimeDefinition()
        let index = PackIndex(ld.Lifetime, pathToDotGit)
        let! oldCommit = Commits.ReadCommit(index, pathToDotGit, commitHash)
        let oldRootTreeHash = oldCommit.Body.Tree
        use inputBlob = new FileStream(fullPathToFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headedBlob = new MemoryStream()
        let blobHash = Objects.WriteObject(GitObjectType.GitBlob, inputBlob, headedBlob)
        Objects.WriteToFile(pathToRepo, headedBlob, blobHash)
        let! treeStreams = Commands.UpdateObjectInTree(index, oldRootTreeHash, pathToDotGit, filePath, blobHash)
        use _ = treeStreams
        let newRootTreeHash = treeStreams.Hashes[0]
        let newCommit = { oldCommit.Body with Tree = newRootTreeHash }
        use inputCommit = new MemoryStream()
        Commits.WriteCommitBody(newCommit, inputCommit)
        inputCommit.Position <- 0L
        use headedCommit = new MemoryStream()
        let newCommitHash = Objects.WriteObject(GitObjectType.GitCommit, inputCommit, headedCommit)
        Objects.WriteToFile(pathToDotGit, headedCommit, newCommitHash)
        Commands.WriteTreeObjects(pathToDotGit, treeStreams)

        if Refs.IsHeadDetached pathToDotGit then
            Refs.UpdateHead(commitHash, newCommitHash, pathToDotGit)
        Refs.UpdateAllRefs(commitHash, newCommitHash, pathToDotGit)
    return ExitCodes.Success
}

let verifyPack (packPath: string) (modeKey: string | null) =
    let packFileName = nonNull <| Path.ChangeExtension(packPath, ".pack")
    if (not (File.Exists(packPath))) then
        printfn $"{packPath} file not found"
        ExitCodes.UnrecognizedArguments
    else
        Commands.VerifyPackFile(LocalPath packFileName, modeKey = "-v")
        |> Seq.iter Console.WriteLine
        ExitCodes.Success
