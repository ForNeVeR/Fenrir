// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Program

open System
open System.IO
open System.Reflection

open System.Threading.Tasks
open Fenrir.Git
open Fenrir.ArgumentCommands
open Fenrir.Git.Metadata
open JetBrains.Lifetimes
open TruePath

let private printVersion() =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn $"v{version}"

let private printUsage() =
    printf @"
Usage:

  guillotine [<input> [<output>]]
    Read git file and write decoded content of the file without header.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  (help | --help)
    Print this message.

  object-type [<path>]
    Prints the type of the Git raw object read from the file system.

    If <path> isn't passed, then accepts raw object contents from the standard input.

  pack [<input> [<output>]]
    Reads the object file passed as <input> and packs the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  refs [<path to .git/ directory>]
    Shows branch list of repository.

  init [<path>]
    Create an empty Git repository or reinitialize an existing one

    If <path to .git/ directory> isn't passed, then current directory are used instead.

  save [<input> [<path to repository>]]
    Read text file and save it as object file to repository.

    If <path to repository> isn't passed, then current directory are used instead.
    If the <input> isn't defined, then standard input are used instead.

  ui [<path>]
    Shows UI to select a commit from the repository identified by <path> (.git subdirectory of current directory by default).

  unpack [<input> [<output>]]
    Unpacks the object file passed as <input> and writes the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  read-commit <path to .git/ directory> <commit-hash>
    Read a commit from a repository and print its metadata.

  print-commits <path to .git/ directory>
    Print all the commits in the repository.

  update-commit <id of commit> [<path to repository>] <path of file>
    Read updated text file from <path to repository>/<path of file> and save it as new object file to repository.
    Moreover, save new trees based on <id of commit> parent tree, its subtrees to repository and commit file.

    If <path to repository> isn't passed, then current directory are used instead.

  update-with-trees <id of root tree> [<path to repository>] <path of file>
    Read updated text file from <path to repository>/<path of file> and save it as new object file to repository.
    Moreover, save new trees based on <id of root tree> tree and its subtrees to repository.

    If <path to repository> isn't passed, then current directory are used instead.

  verify-pack [<path to pack file>]
    Checks pack file integrity and print info about packed object — distribution of delta chains. Use -v option to see all containing objects.

  (version | --version)
    Print the program version.
"

type Task with
    static member RunSynchronously(task: Task): unit =
        task.GetAwaiter().GetResult()
    static member RunSynchronously(task: Task<'a>): 'a =
        task.GetAwaiter().GetResult()

[<EntryPoint>]
let main (argv: string[]): int =
    match argv with
    | [|"guillotine"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        use decodedInput = new MemoryStream()
        Zlib.unpackObject input decodedInput
        decodedInput.Position <- 0L
        let n = Commands.guillotineObject decodedInput output
        printfn $"{string n} bytes have been written"
        ExitCodes.Success
    | [|"guillotine"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = new MemoryStream()
        Zlib.unpackObject input decodedInput
        decodedInput.Position <- 0L
        use output = Console.OpenStandardOutput()
        let n = Commands.guillotineObject decodedInput output
        printfn $"{string n} bytes have been written"
        ExitCodes.Success
    | [|"guillotine"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = new MemoryStream()
        Zlib.unpackObject input decodedInput
        decodedInput.Position <- 0L
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        let n = Commands.guillotineObject decodedInput output
        printfn $"{string n} bytes have been written"
        ExitCodes.Success

    | [|"help"|] | [|"--help"|] | [||] ->
        printUsage()
        ExitCodes.Success

    | [|"object-type"|] ->
        use input = Console.OpenStandardInput()
        let header = Commands.readHeader input
        printfn $"{header}"
        ExitCodes.Success
    | [|"object-type"; inputFilePath|] ->
        use input = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
        let header = Commands.readHeader input
        printfn $"{header}"
        ExitCodes.Success

    | [|"pack"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        Zlib.packObject input output
        ExitCodes.Success
    | [|"pack"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = Console.OpenStandardOutput()
        Zlib.packObject input output
        ExitCodes.Success
    | [|"pack"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        Zlib.packObject input output
        ExitCodes.Success

    | [|"refs"|] ->
        let pathToRepo = AbsolutePath.CurrentWorkingDirectory / ".git"
        Commands.refsCommand(LocalPath pathToRepo)
        ExitCodes.Success
    | [|"refs"; pathToRepo|] ->
        Commands.refsCommand(LocalPath pathToRepo)
        ExitCodes.Success

    | [|"save"|] ->
        let pathToRepo = AbsolutePath.CurrentWorkingDirectory
        use input = Console.OpenStandardInput()
        use headed = new MemoryStream()
        let hashName = Commands.headifyStream GitObjectType.GitBlob input headed
        Commands.writeStreamToFile (LocalPath pathToRepo) headed hashName
        ExitCodes.Success
    | [|"save"; inputPath|] ->
        let pathToRepo = AbsolutePath.CurrentWorkingDirectory
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        let hashName = Commands.headifyStream GitObjectType.GitBlob input headed
        Commands.writeStreamToFile (LocalPath pathToRepo) headed hashName
        ExitCodes.Success
    | [|"save"; inputPath; pathToRepo|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        let hashName = Commands.headifyStream GitObjectType.GitBlob input headed
        Commands.writeStreamToFile (LocalPath pathToRepo) headed hashName
        ExitCodes.Success

    | [|"ui"; path|] ->
        Ui.EntryPoint.run(LocalPath path)
        ExitCodes.Success
    | [|"ui"|] ->
        Ui.EntryPoint.run(LocalPath(AbsolutePath.CurrentWorkingDirectory / ".git"))
        ExitCodes.Success

    | [|"unpack"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        Zlib.unpackObject input output
        ExitCodes.Success
    | [|"unpack"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = Console.OpenStandardOutput()
        Zlib.unpackObject input output
        ExitCodes.Success
    | [|"unpack"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        Zlib.unpackObject input output
        ExitCodes.Success

    | [|"read-commit"; repoPath; commitHash|] ->
        task {
            use ld = new LifetimeDefinition()
            let repoPath = LocalPath repoPath
            let index = PackIndex(ld.Lifetime, repoPath)
            let! commit = Commits.ReadCommit(index, repoPath, Sha1Hash.OfString commitHash)
            printfn $"%A{commit}"
            return ExitCodes.Success
        } |> Task.RunSynchronously

    | [|"print-commits"; gitDir|] ->
        PrintAllCommits(LocalPath gitDir)
        |> Task.RunSynchronously

    | [|"update-commit"; commitHash; filePath|] ->
        let pathToRepo = LocalPath AbsolutePath.CurrentWorkingDirectory
        updateCommitOp (Sha1Hash.OfString commitHash) pathToRepo filePath false
        |> Task.RunSynchronously

    | [|"update-commit"; commitHash; filePath; repoOrForce|] ->
        match repoOrForce.Equals "--force" with
        | true ->
            let pathToRepo = LocalPath AbsolutePath.CurrentWorkingDirectory
            updateCommitOp (Sha1Hash.OfString commitHash) pathToRepo filePath true
        | false ->
            updateCommitOp (Sha1Hash.OfString commitHash) (LocalPath repoOrForce) filePath false
        |> Task.RunSynchronously

    | [|"update-commit"; commitHash; filePath; pathToRepo; forceKey|] ->
        match forceKey.Equals "--force" with
        | true -> updateCommitOp (Sha1Hash.OfString commitHash) (LocalPath pathToRepo) filePath true |> Task.RunSynchronously
        | false -> unrecognizedArgs(argv)

    | [|"update-with-trees"; rootTreeHash; filePath|] ->
        let rootTreeHash = Sha1Hash.OfString rootTreeHash
        let pathToRepo = LocalPath AbsolutePath.CurrentWorkingDirectory
        let pathToDotGit = pathToRepo / ".git"
        let fullPathToFile = pathToRepo / filePath
        use input = new FileStream(fullPathToFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        Commands.writeObjectHeader GitObjectType.GitBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed
        headed.Position <- 0L
        let pathToBlob = Commands.getRawObjectPath pathToDotGit hashName
        Directory.CreateDirectory((pathToDotGit / "objects" / hashName.ToString().Substring(0, 2)).Value) |> ignore
        use output = new FileStream(pathToBlob.Value, FileMode.CreateNew, FileAccess.Write)
        Zlib.packObject headed output
        use ld = new LifetimeDefinition()
        let index = PackIndex(ld.Lifetime, pathToDotGit)
        let tree =
            Commands.updateObjectInTree index rootTreeHash pathToDotGit filePath hashName
            |> Task.RunSynchronously
        Commands.writeTreeObjects pathToRepo tree
        ExitCodes.Success
    | [|"update-with-trees"; rootTreeHash; pathToRepo; filePath|] ->
        let rootTreeHash = Sha1Hash.OfString rootTreeHash
        let pathToDotGit = (LocalPath pathToRepo) / ".git"
        let fullPathToFile = (LocalPath pathToRepo) / filePath
        use input = new FileStream(fullPathToFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        Commands.writeObjectHeader GitObjectType.GitBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed
        headed.Position <- 0L
        let pathToBlob = Commands.getRawObjectPath pathToDotGit hashName
        Directory.CreateDirectory((pathToDotGit / "objects" / hashName.ToString().Substring(0, 2)).Value) |> ignore
        use output = new FileStream(pathToBlob.Value, FileMode.CreateNew, FileAccess.Write)
        Zlib.packObject headed output
        let tree =
            use ld = new LifetimeDefinition()
            let index = PackIndex(ld.Lifetime, pathToDotGit)
            Commands.updateObjectInTree index rootTreeHash pathToDotGit filePath hashName
            |> Task.RunSynchronously

        Commands.writeTreeObjects(LocalPath pathToRepo) tree
        ExitCodes.Success

    | [|"verify-pack"; packPath|] ->
        verifyPack packPath null
    | [|"verify-pack"; packPath; mode|] ->
        verifyPack packPath mode

    | [|"version"|] | [|"--version"|] ->
        printVersion()
        ExitCodes.Success

    | [|"init"|] ->
        let currentDir = Directory.GetCurrentDirectory()
        Commands.createEmptyRepo currentDir
        ExitCodes.Success

    | _ ->
        unrecognizedArgs(argv)
