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
open System.Security.Cryptography

open Fenrir.Git.Metadata
open Fenrir.Git.Packing
open Fenrir.Git.PackVerification
open Fenrir.Git.Tools
open Fenrir.Git.Zlib

let getRawObjectPath (gitDirectoryPath: string) (objectHash: string): string =
    Path.Combine(gitDirectoryPath, "objects", objectHash.Substring(0, 2), objectHash.Substring(2, 38))

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
/// <param name="gitDirectoryPath">Path to the repository's <c>.git</c> directory.</param>
/// <param name="objectHash">Hash of the object.</param>
let readObjectHeader (gitDirectoryPath: string) (objectHash: string): ObjectHeader =
    let rawObjectPath = getRawObjectPath gitDirectoryPath objectHash
    if File.Exists rawObjectPath
    then
        use input = new FileStream(rawObjectPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = input |> getDecodedStream
        readHeader decodedInput
    else
        use packedObject = getPackedObject gitDirectoryPath objectHash
        { Type = packedObject.ObjectType
          Size = Checked.uint64 packedObject.Stream.Length }

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

let refsCommand(path: string): unit =
    Refs.readRefs path
    |> Seq.iter(fun ref -> printfn "%s: %s" ref.Name ref.CommitObjectId)

let getHeadlessCommitBody (decodedInput: MemoryStream): CommitBody =
    let enc = System.Text.Encoding.UTF8
    use sr = new StreamReader(decodedInput, enc)
    let tree = sr.ReadLine().Substring(5)
    let rec parseParents (s : StreamReader) (P : String list) : (String list * String[]) =
        let str = s.ReadLine()
        match str.Substring(0, 7) with
            | "parent " -> parseParents s (List.append P [str.Substring(7, 40)])
            | _         -> (P, [|str|])
    let (p, r) = parseParents sr []
    let rr = (sr.ReadToEnd()).Split "\n" |> Array.append r
    {Tree = tree; Parents = (Array.ofList p); Rest = rr}

let streamToCommitBody (decodedInput: MemoryStream): CommitBody =
    match (readHeader decodedInput).Type with
        | GitObjectType.GitTree   -> failwithf "Found tree file instead of commit file"
        | GitObjectType.GitBlob   -> failwithf "Found blob file instead of commit file"
        | GitObjectType.GitCommit -> getHeadlessCommitBody decodedInput


let parseCommitBody (path : String) (hash : String) : CommitBody =
    let pathToFile = getRawObjectPath path hash
    match File.Exists(pathToFile) with
        | true ->
            use input = new FileStream(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
            use decodedInput = input |> getDecodedStream
            decodedInput |> streamToCommitBody
        | false ->
            use packedObject = getPackedObject path hash
            packedObject.Stream |> getHeadlessCommitBody

let getHeadlessTreeBody (size: uint64) (decodedInput: MemoryStream): TreeBody =
    let bF = new BinaryReader(decodedInput, Encoding.ASCII)
    let rec makeList (n:int): TreeAtom list =
        try
            {Mode = readWhile (fun b -> b <> byte ' ') size bF |> Encoding.ASCII.GetString |> Convert.ToUInt64;
            Name = readWhile (fun b -> b <> 0uy) size bF |> Encoding.ASCII.GetString;
            Hash = bF.ReadBytes(20)} :: makeList (n + 1)
        with
            | :? EndOfStreamException -> []
    makeList 0 |> Array.ofList

let streamToTreeBody (decodedInput: MemoryStream): TreeBody =
    let hd = readHeader decodedInput
    match hd.Type with
        | GitObjectType.GitCommit   -> failwithf "Found commit file instead of tree file"
        | GitObjectType.GitBlob     -> failwithf "Found blob file instead of tree file"
        | GitObjectType.GitTree     -> getHeadlessTreeBody hd.Size decodedInput

/// <summary>Parses a tree object information.</summary>
/// <param name="path">Path to a repository's <c>.git</c> folder.</param>
/// <param name="hash">Hash of the tree object.</param>
let parseTreeBody (path : String) (hash : String) : TreeBody =
    let pathToFile = getRawObjectPath path hash
    match File.Exists(pathToFile) with
        | true ->
            use input = new FileStream(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
            use decodedInput = input |> getDecodedStream
            decodedInput |> streamToTreeBody
        | false ->
            use packedObject = getPackedObject path hash
            packedObject.Stream |> getHeadlessTreeBody (uint64 packedObject.Stream.Length)

let writeObjectHeader (tp: GitObjectType) (input: Stream) (output: Stream): unit =
    match tp with
    | GitObjectType.GitTree   -> output.Write(ReadOnlySpan<byte>("tree "B))
    | GitObjectType.GitCommit -> output.Write(ReadOnlySpan<byte>("commit "B))
    | GitObjectType.GitBlob   -> output.Write(ReadOnlySpan<byte>("blob "B))
    | _                       -> failwithf "Invalid type of Git object"
    output.Write(ReadOnlySpan<byte>(input.Length.ToString(CultureInfo.InvariantCulture)
                                    |> System.Text.Encoding.ASCII.GetBytes))
    output.WriteByte(00uy)

let doAndRewind (action: Stream -> unit): MemoryStream =
    let output = new MemoryStream()
    action output
    output.Position <- 0L
    output

let SHA1 (input: Stream): byte[] =
    use tempStream = input.CopyTo |> doAndRewind
    use sha = new SHA1CryptoServiceProvider()
    sha.ComputeHash(tempStream.ToArray())

let headifyStream (tp: GitObjectType) (input: Stream) (headed: MemoryStream): String =
    writeObjectHeader tp input headed
    input.CopyTo headed
    headed.Position <- 0L
    let hash = SHA1 headed |> byteToString
    headed.Position <- 0L
    hash

let hashOfObjectInTree (tree: TreeBody) (name: String): byte[] =
    let atom = Array.find (fun a -> a.Name = name) tree
    atom.Hash

let changeHashInTree (tree: TreeBody) (hash: byte[]) (name: String): TreeBody =
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
        stream.Write(ReadOnlySpan<byte>(a.Hash))
    Array.iter printAtom tree

let changeHashInCommit (commit: CommitBody) (hash: byte[]): CommitBody =
    {Tree = hash |> byteToString;
     Parents = commit.Parents;
     Rest = commit.Rest}

let commitBodyToStream (commit: CommitBody) (stream: Stream): unit =
    let printParent (a: String): unit =
        stream.Write(ReadOnlySpan<byte>("parent "B))
        stream.Write(ReadOnlySpan<byte>(a |> Encoding.ASCII.GetBytes))
        stream.WriteByte('\n'B)

    stream.Write(ReadOnlySpan<byte>("tree "B))
    stream.Write(ReadOnlySpan<byte>(commit.Tree |> Encoding.ASCII.GetBytes))
    stream.WriteByte('\n'B)

    Array.iter printParent commit.Parents
    stream.Write(ReadOnlySpan<byte>(String.Join('\n', commit.Rest) |> Encoding.ASCII.GetBytes))

type TreeStreams(length: int) =
    member val Streams = Array.init length (fun _ -> new MemoryStream())
    member val Hashes = Array.create length ""
    interface System.IDisposable with
        member this.Dispose() =
            Array.iter (fun (s: MemoryStream) -> s.Dispose()) this.Streams

let updateObjectInTree (rootTreeHash: string) (pathToRepo: String) (filePath: string) (blobHash: string): TreeStreams =
    let filePathList = filePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries) |> List.ofArray
    let treeStreams = new TreeStreams (filePathList.Length)
    let treeToStream (newTree: TreeBody) (index: int): String =
        use input = new MemoryStream()
        treeBodyToStream newTree input
        input.Position <- 0L
        let hash = headifyStream GitObjectType.GitTree input treeStreams.Streams.[index]
        treeStreams.Hashes.[index] <- hash
        hash
    let rec updateFileHashInTree (tree: TreeBody) (filePaths: String list): String =
        let index = treeStreams.Streams.Length - filePaths.Length
        match filePaths with
        | [] -> failwithf "Empty path to file"
        | [fileName] ->
            let newTree = changeHashInTree tree (stringToByte blobHash) fileName
            treeToStream newTree index
        | directoryName :: restPathSegments ->
            let directoryHash = hashOfObjectInTree tree directoryName |> byteToString
            let subTree = parseTreeBody pathToRepo directoryHash
            let newHash = updateFileHashInTree subTree restPathSegments
            let newTree = changeHashInTree tree (stringToByte newHash) directoryName
            treeToStream newTree index

    let parentTree = parseTreeBody pathToRepo rootTreeHash
    updateFileHashInTree parentTree filePathList |> ignore
    treeStreams.Streams |> Array.iter (fun s -> s.Position <- 0L)
    treeStreams

let writeStreamToFile (pathToRepo: string) (stream: MemoryStream) (hash: String) : unit =
    let pathToDirectory = Path.Combine(pathToRepo, ".git", "objects", hash.Substring(0, 2))
    let pathToFile = getRawObjectPath (Path.Combine(pathToRepo, ".git")) hash
    match Directory.Exists(pathToDirectory) with
        | true -> ()
        | false -> Directory.CreateDirectory(pathToDirectory) |> ignore
    match File.Exists(pathToFile) with
        | true -> ()
        | false ->
            use output = new FileStream(pathToFile, FileMode.CreateNew, FileAccess.Write)
            packObject stream output

let writeTreeObjects (pathToRepo: string) (streams: TreeStreams): unit =
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
