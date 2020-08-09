namespace Fenrir

open System

type Ref = {
    Name: string
    CommitObjectId: string
}

module Refs =
    open System.IO

    let isHeadDetached (pathToRepo: string): bool =
        let pathToHead = Path.Combine(pathToRepo, "HEAD")
        not <| File.ReadAllText(pathToHead).StartsWith("ref: refs/heads/")

    let private prependName name ref =
        { ref with Name = sprintf "%s/%s" name ref.Name }

    let rec private readRefsRecursively path =
        Directory.EnumerateFileSystemEntries path
        |> Seq.collect(fun entry ->
            let name = Path.GetFileName entry
            if Directory.Exists entry then
                readRefsRecursively entry
                |> Seq.map(prependName name)
            else
                let commitId = File.ReadLines entry |> Seq.head
                Seq.singleton { Name = name; CommitObjectId = commitId }
        )

    let rec readRefs(repositoryPath: string): Ref seq =
        let refsDirectory = Path.Combine(repositoryPath, "refs")
        readRefsRecursively refsDirectory
        |> Seq.map(prependName "refs")
        |> Seq.sortBy(fun ref -> ref.Name)

    let identifyRefs (commitHash: string) (repositoryPath: string): Ref seq =
        readRefs repositoryPath
        |> Seq.filter (fun item -> item.CommitObjectId.Equals commitHash)

    let updateHead (oldCommit: string) (newCommit: string) (pathToRepo: string): unit =
        let pathToHead = Path.Combine(pathToRepo, "HEAD")
        match (File.ReadAllText pathToHead).StartsWith oldCommit with
        | true -> File.WriteAllText(pathToHead, newCommit)
        | false -> ()

    let updateRef (newCommit: string) (pathToRepo: string) (ref: Ref) : unit =
        let splitName = ref.Name.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                            |> List.ofArray
        let pathToRef = Path.Combine(pathToRepo::splitName |> Array.ofList)
        File.WriteAllText(pathToRef, newCommit)

    let updateAllRefs (oldCommit: string) (newCommit: string) (pathToRepo: string): unit =
        identifyRefs oldCommit pathToRepo |> Seq.iter (updateRef newCommit pathToRepo)
