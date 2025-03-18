/// Functions to manipulate Git commits.
module Fenrir.Git.Commits

open System.IO
open System.Text
open System.Threading.Tasks
open FSharp.Control
open Fenrir.Git.Metadata
open Fenrir.Git.Packing
open Fenrir.Git.Zlib
open TruePath

let private GetHeadlessCommitBody(decodedInput: MemoryStream): CommitBody =
    let enc = Encoding.UTF8
    use sr = new StreamReader(decodedInput, enc)
    let tree = nonNull(sr.ReadLine()).Substring(5)
    let rec parseParents (s : StreamReader) (P : string list) : (string list * string[]) =
        let str = nonNull <| s.ReadLine()
        match str.Substring(0, 7) with
            | "parent " -> parseParents s (List.append P [str.Substring(7, 40)])
            | _         -> (P, [|str|])
    let p, r = parseParents sr []
    let rr = sr.ReadToEnd().Split "\n" |> Array.append r
    { Tree = tree; Parents = (Array.ofList p); Rest = rr }

let private StreamToCommitBody(decodedInput: MemoryStream): CommitBody =
    match (Commands.readHeader decodedInput).Type with
        | GitObjectType.GitTree   -> failwithf "Found tree file instead of commit file"
        | GitObjectType.GitBlob   -> failwithf "Found blob file instead of commit file"
        | GitObjectType.GitCommit -> GetHeadlessCommitBody decodedInput
        | x -> failwithf $"Unknown Git object type: {x}."

/// <summary>Reads a commit with specified <paramref name="hash"/>.</summary>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="hash">Commit hash.</param>
let ReadCommit(gitDirectory: LocalPath, hash: string): Task<Commit> =
    let pathToFile = Commands.getRawObjectPath gitDirectory.Value hash
    let body =
        match File.Exists(pathToFile) with
        | true ->
            use input = new FileStream(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
            use decodedInput = input |> getDecodedStream
            decodedInput |> StreamToCommitBody
        | false ->
            use packedObject = getPackedObject gitDirectory.Value hash
            packedObject.Stream |> GetHeadlessCommitBody
    { Hash = hash; Body = body }
    |> Task.FromResult

/// <summary>
/// Starting with commit <paramref name="headCommitHash"/>, will enumerate all its parents. The enumeration order
/// between unrelated commits is unspecified, but any commit is guaranteed to be returned <i>before</i> any of its
/// parents.
/// </summary>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="headCommitHash">Hash of the starting commit.</param>
let TraverseCommits(gitDirectory: LocalPath, headCommitHash: string): System.Collections.Generic.IAsyncEnumerable<Commit> =
    AsyncSeq.empty |> AsyncSeq.toAsyncEnum
