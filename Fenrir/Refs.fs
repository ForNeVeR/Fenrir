namespace Fenrir

open System

type Ref = {
    Name: string
    CommitObjectId: string
}

module Refs =
    open System.IO

    let isHeadDetached (pathDotGit: string): bool =
        let pathToHead = Path.Combine(pathDotGit, "HEAD")
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


    let private readPackedRefs (repositoryPath:string) :Ref seq=
        let pathToPackedRefs = Path.Combine(repositoryPath, "packed-refs")
        if File.Exists pathToPackedRefs
        then
            let packedRefsLines =  File.ReadAllLines(pathToPackedRefs)
            Array.filter (fun (str : string) -> not(str.StartsWith('#') || str.StartsWith('^'))) packedRefsLines
            |> Seq.collect (fun entryString ->
            let commitAndName = entryString.Split(' ')
            Seq.singleton {Name = commitAndName.[1]; CommitObjectId = commitAndName.[0]}
            )
        else
            Seq.empty


    let rec readRefs(repositoryPath: string): Ref seq =
        let refsDirectory = Path.Combine(repositoryPath, "refs")
        let packedRefs = readPackedRefs repositoryPath

        readRefsRecursively refsDirectory
        |> Seq.map(prependName "refs")
        |> Seq.append packedRefs
        |> Seq.sortBy(fun ref -> ref.Name)

        
        


    let identifyRefs (commitHash: string) (repositoryPath: string): Ref seq =
        readRefs repositoryPath
        |> Seq.filter (fun item -> item.CommitObjectId.Equals commitHash)

    let updateHead (oldCommit: string) (newCommit: string) (pathDotGit: string): unit =
        let pathToHead = Path.Combine(pathDotGit, "HEAD")
        match (File.ReadAllText pathToHead).StartsWith oldCommit with
        | true -> File.WriteAllText(pathToHead, newCommit)
        | false -> ()

    let updateRef (newCommit: string) (pathDotGit: string) (ref: Ref) : unit =
        let splitName = ref.Name.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                            |> List.ofArray
        let pathToRef = Path.Combine(pathDotGit::splitName |> Array.ofList)
        File.WriteAllText(pathToRef, newCommit)

    let updateAllRefs (oldCommit: string) (newCommit: string) (pathDotGit: string): unit =
        identifyRefs oldCommit pathDotGit |> Seq.iter (updateRef newCommit pathDotGit)
