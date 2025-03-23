// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// Various high-level commands to operate on a Git repository.
module Fenrir.Git.Commands

open System
open System.IO

open System.Threading.Tasks
open Fenrir.Git.Metadata
open TruePath

/// A collection of streams containing objects in a tree.
type TreeStreams(length: int) =
    member val Streams = Array.init length (fun _ -> new MemoryStream())
    member val Hashes: Sha1Hash[] = Array.create length Sha1Hash.Zero
    interface IDisposable with
        member this.Dispose() =
            Array.iter (fun (s: MemoryStream) -> s.Dispose()) this.Streams

/// <summary>Update a particular object in a tree, while traversing nested trees.</summary>
/// <param name="packIndex">Pack index for faster pack access.</param>
/// <param name="rootTreeHash">Hash of the root tree that should be processed.</param>
/// <param name="gitDirectory">Path to repository's <c>./git</c> directory.</param>
/// <param name="filePath">Path to the file that needs to be processed.</param>
/// <param name="blobHash">Hash of the object that should replace the file by <paramref name="filePath"/>.</param>
/// <returns>A collection of streams with all the modified trees.</returns>
let UpdateObjectInTree(packIndex: PackIndex, rootTreeHash: Sha1Hash, gitDirectory: LocalPath, filePath: string, blobHash: Sha1Hash): Task<TreeStreams> =
    let filePathList = filePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries) |> List.ofArray
    let treeStreams = new TreeStreams (filePathList.Length)
    let treeToStream (newTree: TreeBody) (index: int): Sha1Hash =
        use input = new MemoryStream()
        Trees.WriteTreeBody(newTree, input)
        input.Position <- 0L
        let hash = Objects.WriteObject(GitObjectType.GitTree, input, treeStreams.Streams[index])
        treeStreams.Hashes[index] <- hash
        hash
    let rec updateFileHashInTree (tree: TreeBody) (filePaths: String list) = task {
        let index = treeStreams.Streams.Length - filePaths.Length
        match filePaths with
        | [] -> return failwithf "Empty path to file"
        | [fileName] ->
            let newTree = Trees.ChangeHashInTree tree blobHash fileName
            return treeToStream newTree index
        | directoryName :: restPathSegments ->
            let directoryHash = Trees.HashOfObjectInTree(tree, directoryName)
            let! subTree = Trees.ReadTreeBody(packIndex, gitDirectory, directoryHash)
            let! newHash = updateFileHashInTree subTree restPathSegments
            let newTree = Trees.ChangeHashInTree tree newHash directoryName
            return treeToStream newTree index
    }

    task {
        let! parentTree = Trees.ReadTreeBody(packIndex, gitDirectory, rootTreeHash)
        updateFileHashInTree parentTree filePathList |> ignore
        treeStreams.Streams |> Array.iter (fun s -> s.Position <- 0L)
        return treeStreams
    }

/// <summary>Saves a collection of tree objects to the repository.</summary>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="streams">A collection of tree objects.</param>
let WriteTreeObjects(gitDirectory: LocalPath, streams: TreeStreams): unit =
    Array.iter2 (fun s h -> Objects.WriteToFile(gitDirectory, s, h)) streams.Streams streams.Hashes

/// <summary>Initialize an empty Git repository.  A <c>.git</c> subdirectory will be created.</summary>
/// <param name="repositoryPath">Path to the repository folder.</param>
let InitializeRepository(repositoryPath: LocalPath): unit =
    let gitPath = repositoryPath / ".git"

    if Directory.Exists gitPath.Value
    then failwithf $".git folder already exist in \"{repositoryPath}\" directory."

    let gitDir = Directory.CreateDirectory gitPath.Value

    gitDir.Attributes <- FileAttributes.Directory ||| FileAttributes.Hidden
    File.WriteAllLines(Path.Combine(gitDir.FullName,"HEAD"),[|"ref: refs/heads/master"|])
    File.WriteAllLines(Path.Combine(gitDir.FullName,"description"),[|"Unnamed repository; edit this file 'description' to name the repository."|])
    File.WriteAllLines(Path.Combine(gitDir.FullName,"config"),[|
    "[core]"
    "\trepositoryformatversion = 0"
    "\tfilemode = false"
    "\tbare = false"
    "\tlogallrefupdates = true"
    "\tsymlinks = false"
    "\tignorecase = true"
    |])

    let _hooksDir = gitDir.CreateSubdirectory("hooks")

    let refsDir = gitDir.CreateSubdirectory("refs" )
    refsDir.CreateSubdirectory("heads") |> ignore
    refsDir.CreateSubdirectory("tags") |>ignore

    let objDir = gitDir.CreateSubdirectory("objects")
    objDir.CreateSubdirectory("info") |> ignore
    objDir.CreateSubdirectory("pack") |> ignore

    let infoDir = gitDir.CreateSubdirectory("info")
    File.WriteAllLines(Path.Combine(infoDir.FullName,"exclude"),[|
    "# git ls-files --others --exclude-from=.git/info/exclude"
    "# Lines that start with '#' are comments."
    "# For a project mostly in C, the following would be a good set of"
    "# exclude patterns (uncomment them if you want to use them):"
    "# *.[oa]"
    "# *~"
    |])

/// <summary>Verify a pack file.</summary>
/// <param name="packFile">Path to the pack file.</param>
/// <param name="verbose">Verbosity flag.</param>
/// <returns>Pack file verification report.</returns>
let VerifyPackFile(packFile: LocalPath, verbose: bool): seq<string> =
    use reader = new BinaryReader(File.Open(packFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read))
    PackVerification.Verify reader verbose
