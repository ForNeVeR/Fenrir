// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Git

open System
open System.Collections.Generic
open System.Threading.Tasks
open TruePath

/// <summary>A Git <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-References">reference</a>.</summary>
type Ref = {
    /// <summary>Reference name. Might be <c>null</c> in case of detached commit.</summary>
    Name: string | null
    /// Commit the reference points to.
    CommitObjectId: Sha1Hash
}

/// <summary>
/// Functions to operate on Git <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-References">references</a>.
/// </summary>
module Refs =
    open System.IO

    /// <summary>Determines if the repository is in the detached head state.</summary>
    /// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
    let IsHeadDetached(gitDirectory: LocalPath): bool =
        let pathToHead = gitDirectory / "HEAD"
        not <| File.ReadAllText(pathToHead.Value).StartsWith("ref: refs/heads/")

    let private prependName (name: string) ref =
        { ref with Name = $"{name}/{ref.Name}" }

    /// <remarks>
    /// See
    /// <a href="https://github.com/git/git/blob/028f618658e34230e1d65678f14b6876e0f9856d/refs/files-backend.c#L608"><c>parse_loose_ref_contents</c></a>
    /// in Git's code.
    /// </remarks>
    let private ParseSymbolicRef(symbolicRef: string): ValueOption<string> =
        // https://en.cppreference.com/w/c/string/byte/isspace
        let isAnsiSpace =
            let ansiSpaces = HashSet([|
                ' '
                '\u000c' // \f
                '\u000a' // \n
                '\u000d' // \r
                '\u0009' // \t
                '\u000b' // \v
            |])
            ansiSpaces.Contains

        if symbolicRef.StartsWith("ref:") then
            let mutable span = symbolicRef.AsSpan().Slice("ref:".Length)
            while span.Length > 0 && isAnsiSpace span[0] do
                span <- span.Slice 1

            if span.IsEmpty then
                failwithf $"Cannot read symbolic ref from content: \"{symbolicRef}\"."

            ValueSome <| String(span).TrimEnd()
        else ValueNone

    let private resolveSymbolicReference (gitDirectoryPath: LocalPath) (symbolicRef: string) : string=
        let pathToRef = gitDirectoryPath / symbolicRef
        (File.ReadAllLines pathToRef.Value)[0]

    let rec private readRefsRecursively (path: LocalPath) (repositoryPath: LocalPath) =
        Directory.EnumerateFileSystemEntries path.Value
        |> Seq.collect(fun entry ->
            let name = nonNull <| Path.GetFileName entry
            if Directory.Exists entry then
                readRefsRecursively (LocalPath entry) repositoryPath
                |> Seq.map(prependName name)
            else
                let commitOrRef = File.ReadLines entry |> Seq.head
                let commitId =
                    ParseSymbolicRef commitOrRef
                    |> ValueOption.map(resolveSymbolicReference repositoryPath)
                    |> ValueOption.defaultValue commitOrRef
                    |> Sha1Hash.OfHexString

                Seq.singleton { Name = name; CommitObjectId = commitId }
        )


    let private readPackedRefs(repositoryPath: LocalPath): Ref seq =
        let pathToPackedRefs = repositoryPath / "packed-refs"
        if File.Exists pathToPackedRefs.Value
        then
            let packedRefsLines = File.ReadAllLines pathToPackedRefs.Value
            Array.filter (fun (str : string) -> not(str.StartsWith('#') || str.StartsWith('^'))) packedRefsLines
            |> Seq.collect (fun entryString ->
            let commitAndName = entryString.Split(' ')
            Seq.singleton {
                Name = commitAndName[1]
                CommitObjectId = commitAndName[0]  |> Sha1Hash.OfHexString
            }
            )
        else
            Seq.empty


    /// <summary>Reads the list of references available in a repository.</summary>
    /// <param name="repositoryPath">Path to a repository's <c>.git</c> directory.</param>
    /// <remarks>This function supports both packed and unpacked refs.</remarks>
    let rec ReadRefs(repositoryPath: LocalPath): Ref seq =
        let refsDirectory = repositoryPath / "refs"
        let packedRefs = readPackedRefs repositoryPath

        readRefsRecursively refsDirectory repositoryPath
        |> Seq.map(prependName "refs")
        |> Seq.append packedRefs
        |> Seq.sortBy(fun ref -> ref.Name)

    /// <summary>Reads a reference from the <c>HEAD</c> file in the repository.</summary>
    /// <param name="gitDirectory">Path to the repository's <c>.git</c> directory.</param>
    /// <returns>Reference if it's resolved, <c>null</c> if the <c>HEAD</c> file doesn't exist.</returns>
    let ReadHead(gitDirectory: LocalPath): Task<Ref | null> = task {
        let headFile = gitDirectory / "HEAD"
        if not <| File.Exists headFile.Value then return null
        else

        let! headFileContent = File.ReadAllTextAsync headFile.Value
        return
            ParseSymbolicRef headFileContent
            |> ValueOption.map(fun ref ->
                let commitHash = resolveSymbolicReference gitDirectory ref |> Sha1Hash.OfHexString
                { Name = ref; CommitObjectId = commitHash }
            )
            |> ValueOption.defaultWith(fun() ->
                { Name = null; CommitObjectId = headFileContent.TrimEnd() |> Sha1Hash.OfHexString }
            )
    }

    let internal IdentifyRefs (commitHash: Sha1Hash) (repositoryPath: LocalPath): Ref seq =
        ReadRefs repositoryPath
        |> Seq.filter(fun item -> item.CommitObjectId = commitHash)

    /// <summary>
    /// Update the <c>HEAD</c>all reference to point to the new commit <b>if</b> it points to the old commit.
    /// </summary>
    /// <param name="oldCommit">Old commit to update from.</param>
    /// <param name="newCommit">New commit to update to.</param>
    /// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
    let UpdateHead(oldCommit: Sha1Hash, newCommit: Sha1Hash, gitDirectory: LocalPath): unit =
        let pathToHead = gitDirectory / "HEAD"
        if File.ReadAllText(pathToHead.Value).StartsWith(oldCommit.ToString(), StringComparison.OrdinalIgnoreCase) then
            File.WriteAllText(pathToHead.Value, newCommit.ToString())

    let private updateRef (newCommit: Sha1Hash) (gitDirectory: LocalPath) (ref: Ref) : unit =
        let splitName = (nonNull ref.Name).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                            |> List.ofArray
        let pathToRef = Path.Combine(gitDirectory.Value::splitName |> Array.ofList)
        File.WriteAllText(pathToRef, newCommit.ToString())

    /// <summary>Update all refs pointing to the specified commit to point to the new commit.</summary>
    /// <param name="oldCommit">Old commit to update from.</param>
    /// <param name="newCommit">New commit to update to.</param>
    /// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
    let UpdateAllRefs(oldCommit: Sha1Hash, newCommit: Sha1Hash, gitDirectory: LocalPath): unit =
        IdentifyRefs oldCommit gitDirectory |> Seq.iter (updateRef newCommit gitDirectory)
