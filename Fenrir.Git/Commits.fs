/// Functions to manipulate Git commits.
module Fenrir.Git.Commits

open System.Collections.Generic
open System.IO
open System.Text
open System.Threading.Tasks
open FSharp.Control
open Fenrir.Git.Metadata
open Fenrir.Git.Packing
open Fenrir.Git.Zlib
open TruePath

let private GetHeadlessCommitBody(decodedInput: Stream): CommitBody =
    let enc = Encoding.UTF8
    use sr = new StreamReader(decodedInput, enc)
    let tree = nonNull(sr.ReadLine()).Substring(5)
    let rec parseParents (s : StreamReader) (P : string list) : string list * string[] =
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
/// <param name="index">Pack file index of the repository.</param>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="hash">Commit hash.</param>
let ReadCommit(index: PackIndex, gitDirectory: LocalPath, hash: string): Task<Commit> = task {
    let pathToFile = Commands.getRawObjectPath gitDirectory hash
    let! body = task {
        match File.Exists pathToFile.Value with
        | true ->
            use input = new FileStream(pathToFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
            use decodedInput = input |> getDecodedStream
            return decodedInput |> StreamToCommitBody
        | false ->
            let! packedObject = ReadPackedObject(index, hash)
            use po = nonNull packedObject
            return po.Stream |> GetHeadlessCommitBody
    }
    return { Hash = hash; Body = body }
}

/// <summary>
/// Starting with commit <paramref name="headCommitHash"/>, will enumerate all its parents. The enumeration order
/// between commits not related via parent-child relationship is unspecified, but any commit is guaranteed to be
/// returned <i>before</i> any of its parents.
/// </summary>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="headCommitHash">Hash of the starting commit.</param>
let TraverseCommits(gitDirectory: LocalPath, headCommitHash: string): System.Collections.Generic.IAsyncEnumerable<Commit> =
    let index = PackIndex gitDirectory
    asyncSeq {
        let visitedCommits = HashSet()
        let currentCommits = Stack [| headCommitHash |]
        while currentCommits.Count > 0 do
            let commitHash = currentCommits.Pop()
            if visitedCommits.Add commitHash then
                let! commit = Async.AwaitTask <| ReadCommit(index, gitDirectory, commitHash)
                yield commit
                commit.Body.Parents |> Array.iter currentCommits.Push
    } |> AsyncSeq.toAsyncEnum
