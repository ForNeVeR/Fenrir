// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Git

open System
open System.Collections.Generic
open System.Threading.Tasks
open TruePath

type Ref = {
    /// <summary>Reference name. Might be <c>null</c> in case of detached commit.</summary>
    Name: string | null
    CommitObjectId: string
}

/// <summary>
/// Functions to operate on Git <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-References">references</a>.
/// </summary>
module Refs =
    open System.IO

    let isHeadDetached (pathDotGit: string): bool =
        let pathToHead = Path.Combine(pathDotGit, "HEAD")
        not <| File.ReadAllText(pathToHead).StartsWith("ref: refs/heads/")

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

    let private resolveSymbolicReference (gitDirectoryPath : string) (symbolicRef: string) : string=
        let pathToRef = Path.Combine(gitDirectoryPath, symbolicRef)
        (File.ReadAllLines pathToRef)[0]

    let rec private readRefsRecursively path repositoryPath =
        Directory.EnumerateFileSystemEntries path
        |> Seq.collect(fun entry ->
            let name = nonNull <| Path.GetFileName entry
            if Directory.Exists entry then
                readRefsRecursively entry repositoryPath
                |> Seq.map(prependName name)
            else
                let commitOrRef = File.ReadLines entry |> Seq.head
                let commitId =
                    ParseSymbolicRef commitOrRef
                    |> ValueOption.map(resolveSymbolicReference repositoryPath)
                    |> ValueOption.defaultValue commitOrRef

                Seq.singleton { Name = name; CommitObjectId = commitId }
        )


    let private readPackedRefs (repositoryPath:string) :Ref seq=
        let pathToPackedRefs = Path.Combine(repositoryPath, "packed-refs")
        if File.Exists pathToPackedRefs
        then
            let packedRefsLines =  File.ReadAllLines(pathToPackedRefs)
            Array.filter (fun (str : string) -> not(str.StartsWith('#') || str.StartsWith('^'))) packedRefsLines
            |> Seq.collect (fun entryString ->
            let commitAndName = entryString.Split(' ')
            Seq.singleton {Name = commitAndName[1]; CommitObjectId = commitAndName[0]}
            )
        else
            Seq.empty


    /// <summary>Reads the list of references available in a repository.</summary>
    /// <param name="repositoryPath">Path to a repository's <c>.git</c> directory.</param>
    /// <remarks>This function supports both packed and unpacked refs.</remarks>
    let rec readRefs(repositoryPath: string): Ref seq =
        let refsDirectory = Path.Combine(repositoryPath, "refs")
        let packedRefs = readPackedRefs repositoryPath

        readRefsRecursively refsDirectory repositoryPath
        |> Seq.map(prependName "refs")
        |> Seq.append packedRefs
        |> Seq.sortBy(fun ref -> ref.Name)

    /// <summary>Reads a reference from the <c>HEAD</c> file in the repository.</summary>
    /// <param name="gitDirectory">Path to the repository's <c>.git</c> directory.</param>
    /// <returns>Reference if it's resolved, <c>null</c> if the <c>HEAD</c> file doesn't exist.</returns>
    let ReadHeadRef(gitDirectory: LocalPath): Task<Ref | null> = task {
        let headFile = gitDirectory / "HEAD"
        if not <| File.Exists headFile.Value then return null
        else

        let! headFileContent = File.ReadAllTextAsync headFile.Value
        return
            ParseSymbolicRef headFileContent
            |> ValueOption.map(fun ref ->
                let commitHash = resolveSymbolicReference gitDirectory.Value ref
                { Name = ref; CommitObjectId = commitHash }
            )
            |> ValueOption.defaultWith(fun() -> { Name = null; CommitObjectId = headFileContent.TrimEnd() })
    }

    let identifyRefs (commitHash: string) (repositoryPath: string): Ref seq =
        readRefs repositoryPath
        |> Seq.filter (fun item -> item.CommitObjectId.Equals commitHash)

    let updateHead (oldCommit: string) (newCommit: string) (pathDotGit: string): unit =
        let pathToHead = Path.Combine(pathDotGit, "HEAD")
        match (File.ReadAllText pathToHead).StartsWith oldCommit with
        | true -> File.WriteAllText(pathToHead, newCommit)
        | false -> ()

    let updateRef (newCommit: string) (pathDotGit: string) (ref: Ref) : unit =
        let splitName = (nonNull ref.Name).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                            |> List.ofArray
        let pathToRef = Path.Combine(pathDotGit::splitName |> Array.ofList)
        File.WriteAllText(pathToRef, newCommit)

    let updateAllRefs (oldCommit: string) (newCommit: string) (pathDotGit: string): unit =
        identifyRefs oldCommit pathDotGit |> Seq.iter (updateRef newCommit pathDotGit)
