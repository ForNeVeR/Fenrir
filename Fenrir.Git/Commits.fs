// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// Functions to manipulate Git commits.
module Fenrir.Git.Commits

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading.Tasks
open FSharp.Control
open Fenrir.Git.Metadata
open Fenrir.Git.Packing
open Fenrir.Git.Zlib
open JetBrains.Lifetimes
open TruePath

let private GetHeadlessCommitBody(decodedInput: Stream): CommitBody =
    let enc = Encoding.UTF8
    use sr = new StreamReader(decodedInput, enc)
    let tree = nonNull(sr.ReadLine()).Substring(5) |> Sha1Hash.OfHexString
    let rec parseParents (s : StreamReader) (P : Sha1Hash list) : Sha1Hash list * string[] =
        let str = nonNull <| s.ReadLine()
        match str.Substring(0, 7) with
            | "parent " -> parseParents s (List.append P [Sha1Hash.OfHexString <| str.Substring(7, 40)])
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

/// Writes the commit body's internal representation as a Git object to a passed stream.
let CommitBodyToStream (commit: CommitBody) (stream: Stream): unit =
    let printParent(hash: Sha1Hash): unit =
        stream.Write(ReadOnlySpan<byte>("parent "B))
        stream.Write((hash.ToString() |> Encoding.UTF8.GetBytes).AsSpan())
        stream.WriteByte('\n'B)

    stream.Write(ReadOnlySpan<byte>("tree "B))
    stream.Write(ReadOnlySpan<byte>(commit.Tree.ToString() |> Encoding.ASCII.GetBytes))
    stream.WriteByte('\n'B)

    Array.iter printParent commit.Parents
    stream.Write(ReadOnlySpan<byte>(String.Join('\n', commit.Rest) |> Encoding.ASCII.GetBytes))

/// <summary>Reads a commit with specified <paramref name="hash"/>.</summary>
/// <param name="index">Pack file index of the repository.</param>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="hash">Commit hash.</param>
let ReadCommit(index: PackIndex, gitDirectory: LocalPath, hash: Sha1Hash): Task<Commit> = task {
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
let TraverseCommits(gitDirectory: LocalPath, headCommitHash: Sha1Hash): System.Collections.Generic.IAsyncEnumerable<Commit> =
    asyncSeq {
        use ld = new LifetimeDefinition()
        let index = PackIndex(ld.Lifetime, gitDirectory)
        let visitedCommits = HashSet()
        let currentCommits = Stack [| headCommitHash |]
        while currentCommits.Count > 0 do
            let commitHash = currentCommits.Pop()
            if visitedCommits.Add commitHash then
                let! commit = Async.AwaitTask <| ReadCommit(index, gitDirectory, commitHash)
                yield commit
                commit.Body.Parents |> Array.iter currentCommits.Push
    } |> AsyncSeq.toAsyncEnum
