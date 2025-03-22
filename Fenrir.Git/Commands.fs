// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// Various (relatively) high-level commands to operate on a Git repository.
// TODO[#100]: Separate the commands only useful in the application (e.g. those that print something) and store them outside
// of the library.
module Fenrir.Git.Commands

open System
open System.Text
open System.Globalization
open System.IO

open System.Threading.Tasks
open Fenrir.Git.Metadata
open Fenrir.Git.Packing
open Fenrir.Git.PackVerification
open Fenrir.Git.Tools
open Fenrir.Git.Zlib
open TruePath

let getRawObjectPath (gitDirectoryPath: LocalPath) (objectHash: Sha1Hash): LocalPath =
    let objectHash = objectHash.ToString()
    gitDirectoryPath / "objects" / objectHash.Substring(0, 2) / objectHash.Substring(2, 38)

let readHeader(input: Stream): ObjectHeader =
    let bF = new BinaryReader(input, Encoding.ASCII)

    let maxTypeNameLength = uint64 "commit".Length
    let typeArray = readWhile (fun b -> b <> byte ' ') maxTypeNameLength bF
    let tp =
        match typeArray with
        | "tree"B   -> GitObjectType.GitTree
        | "commit"B -> GitObjectType.GitCommit
        | "blob"B   -> GitObjectType.GitBlob
        | _         -> failwithf "Invalid Git object header"

    let maxLength = uint64 (string UInt64.MaxValue).Length
    let sizeArray = readWhile (fun b -> b <> 0uy) maxLength bF
    let sz = Convert.ToUInt64(System.Text.Encoding.ASCII.GetString(sizeArray), CultureInfo.InvariantCulture)

    {Type = tp; Size = sz}

/// <summary>Reads the header of any object in the Git storage.</summary>
/// <param name="index">Git pack index to search the objects in.</param>
/// <param name="gitDirectoryPath">Path to the repository's <c>.git</c> directory.</param>
/// <param name="objectHash">Hash of the object.</param>
let ReadObjectHeader (index: PackIndex) (gitDirectoryPath: LocalPath) (objectHash: Sha1Hash): Task<ObjectHeader> =
    let rawObjectPath = getRawObjectPath gitDirectoryPath objectHash
    if File.Exists rawObjectPath.Value
    then
        use input = new FileStream(rawObjectPath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = input |> getDecodedStream
        Task.FromResult <| readHeader decodedInput
    else task {
        let! packedObject = ReadPackedObject(index, objectHash)
        use po = nonNull packedObject

        return {
            Type = po.ObjectType
            Size = Checked.uint64 po.Stream.Length
        }
    }

let guillotineObject (input: Stream) (output: Stream): int =
    readHeader input |> ignore
    let bR = new BinaryReader(input)
    let bW = new BinaryWriter(output)
    let rec rewrite n:int =
        try
            bW.Write(bR.ReadByte())
            rewrite (n + 1)
        with
            | :? EndOfStreamException -> n
    rewrite 0

let refsCommand(path: LocalPath): unit =
    Refs.readRefs path
    |> Seq.iter(fun ref -> printfn $"%s{ref.Name}: {ref.CommitObjectId}")

let getHeadlessTreeBody (size: uint64) (decodedInput: Stream): TreeBody =
    let bF = new BinaryReader(decodedInput, Encoding.ASCII)
    let rec makeList (n:int): TreeAtom list =
        try
            {
                Mode = readWhile (fun b -> b <> byte ' ') size bF |> Encoding.ASCII.GetString |> Convert.ToUInt64;
                Name = readWhile (fun b -> b <> 0uy) size bF |> Encoding.ASCII.GetString;
                Hash = Sha1Hash.OfBytes <| bF.ReadBytes(20)
            } :: makeList (n + 1)
        with
            | :? EndOfStreamException -> []
    makeList 0 |> Array.ofList

let streamToTreeBody (decodedInput: MemoryStream): TreeBody =
    let hd = readHeader decodedInput
    match hd.Type with
        | GitObjectType.GitCommit   -> failwithf "Found commit file instead of tree file"
        | GitObjectType.GitBlob     -> failwithf "Found blob file instead of tree file"
        | GitObjectType.GitTree     -> getHeadlessTreeBody hd.Size decodedInput
        | x -> failwithf $"Unknown Git object type: {x}."

/// <summary>Parses a tree object information.</summary>
/// <param name="index">Git pack index to search the objects in.</param>
/// <param name="path">Path to a repository's <c>.git</c> folder.</param>
/// <param name="hash">Hash of the tree object.</param>
let ParseTreeBody (index: PackIndex) (path: LocalPath) (hash: Sha1Hash): Task<TreeBody> =
    let pathToFile = getRawObjectPath path hash
    match File.Exists pathToFile.Value with
        | true ->
            use input = new FileStream(pathToFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
            use decodedInput = input |> getDecodedStream
            Task.FromResult(decodedInput |> streamToTreeBody)
        | false -> task {
            let! packedObject = ReadPackedObject(index, hash)
            use po = nonNull packedObject
            return po.Stream |> getHeadlessTreeBody (uint64 po.Stream.Length)
        }

let writeObjectHeader (tp: GitObjectType) (input: Stream) (output: Stream): unit =
    match tp with
    | GitObjectType.GitTree   -> output.Write(ReadOnlySpan<byte>("tree "B))
    | GitObjectType.GitCommit -> output.Write(ReadOnlySpan<byte>("commit "B))
    | GitObjectType.GitBlob   -> output.Write(ReadOnlySpan<byte>("blob "B))
    | _                       -> failwithf "Invalid type of Git object"
    output.Write(ReadOnlySpan<byte>(input.Length.ToString(CultureInfo.InvariantCulture)
                                    |> Encoding.ASCII.GetBytes))
    output.WriteByte(00uy)

let doAndRewind (action: Stream -> unit): MemoryStream =
    let output = new MemoryStream()
    action output
    output.Position <- 0L
    output

let rec SHA1 (input: Stream): Sha1Hash =
    // TODO[#115]: Use the hardened Sha1 module and not the system implementation.
    use tempStream = input.CopyTo |> doAndRewind
    use sha = System.Security.Cryptography.SHA1.Create()
    sha.ComputeHash(tempStream.ToArray())
    |> Sha1Hash.OfBytes

let headifyStream (tp: GitObjectType) (input: Stream) (headed: MemoryStream): Sha1Hash =
    writeObjectHeader tp input headed
    input.CopyTo headed
    headed.Position <- 0L
    let hash = SHA1 headed
    headed.Position <- 0L
    hash

let hashOfObjectInTree (tree: TreeBody) (name: String): Sha1Hash =
    let atom = Array.find (fun a -> a.Name = name) tree
    atom.Hash

let changeHashInTree (tree: TreeBody) (hash: Sha1Hash) (name: String): TreeBody =
    let changer (a: TreeAtom) : TreeAtom =
        match (a.Name = name) with
            | true -> {Mode = a.Mode; Name = a.Name; Hash = hash}
            | false -> a
    Array.map changer tree

let treeBodyToStream (tree: TreeBody) (stream: Stream): unit =
    let printAtom (a : TreeAtom): unit =
        stream.Write(ReadOnlySpan<byte>(a.Mode.ToString(CultureInfo.InvariantCulture) |> Encoding.ASCII.GetBytes))
        stream.WriteByte(' 'B)
        stream.Write(ReadOnlySpan<byte>(a.Name |> Encoding.ASCII.GetBytes))
        stream.WriteByte(00uy)
        stream.Write(a.Hash.ToBytes())
    Array.iter printAtom tree

let changeHashInCommit (commit: CommitBody) (hash: Sha1Hash): CommitBody =  {
    Tree = hash
    Parents = commit.Parents
    Rest = commit.Rest
}

type TreeStreams(length: int) =
    member val Streams = Array.init length (fun _ -> new MemoryStream())
    member val Hashes: Sha1Hash[] = Array.create length Sha1Hash.Zero
    interface IDisposable with
        member this.Dispose() =
            Array.iter (fun (s: MemoryStream) -> s.Dispose()) this.Streams

let updateObjectInTree (packIndex: PackIndex)
                       (rootTreeHash: Sha1Hash)
                       (pathToRepo: LocalPath)
                       (filePath: string)
                       (blobHash: Sha1Hash): Task<TreeStreams> =
    let filePathList = filePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries) |> List.ofArray
    let treeStreams = new TreeStreams (filePathList.Length)
    let treeToStream (newTree: TreeBody) (index: int): Sha1Hash =
        use input = new MemoryStream()
        treeBodyToStream newTree input
        input.Position <- 0L
        let hash = headifyStream GitObjectType.GitTree input treeStreams.Streams.[index]
        treeStreams.Hashes.[index] <- hash
        hash
    let rec updateFileHashInTree (tree: TreeBody) (filePaths: String list) = task {
        let index = treeStreams.Streams.Length - filePaths.Length
        match filePaths with
        | [] -> return failwithf "Empty path to file"
        | [fileName] ->
            let newTree = changeHashInTree tree blobHash fileName
            return treeToStream newTree index
        | directoryName :: restPathSegments ->
            let directoryHash = hashOfObjectInTree tree directoryName
            let! subTree = ParseTreeBody packIndex pathToRepo directoryHash
            let! newHash = updateFileHashInTree subTree restPathSegments
            let newTree = changeHashInTree tree newHash directoryName
            return treeToStream newTree index
    }

    task {
        let! parentTree = ParseTreeBody packIndex pathToRepo rootTreeHash
        updateFileHashInTree parentTree filePathList |> ignore
        treeStreams.Streams |> Array.iter (fun s -> s.Position <- 0L)
        return treeStreams
    }

let writeStreamToFile (pathToRepo: LocalPath) (stream: MemoryStream) (hash: Sha1Hash) : unit =
    let pathToDirectory = pathToRepo / ".git" / "objects" / hash.ToString().Substring(0, 2)
    let pathToFile = getRawObjectPath (pathToRepo / ".git") hash
    match Directory.Exists pathToDirectory.Value with
        | true -> ()
        | false -> Directory.CreateDirectory pathToDirectory.Value |> ignore
    match File.Exists pathToFile.Value with
        | true -> ()
        | false ->
            use output = new FileStream(pathToFile.Value, FileMode.CreateNew, FileAccess.Write)
            packObject stream output

let writeTreeObjects (pathToRepo: LocalPath) (streams: TreeStreams): unit =
    Array.iter2 (writeStreamToFile pathToRepo) streams.Streams streams.Hashes

let createEmptyRepo (pathWhereInit : string) :unit =
    if (Path.Combine(pathWhereInit,".git") |> Directory.Exists)
    then failwith(".git folder already exist in " + pathWhereInit + " directory")

    let gitDir = Path.Combine(pathWhereInit,".git") |> Directory.CreateDirectory

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

    let hooksDir = gitDir.CreateSubdirectory("hooks")

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

let verifyPack (pathToFile:string) (verbose: bool) : seq<string> =
    use reader = new BinaryReader(File.Open(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read))
    verifyPack reader verbose
