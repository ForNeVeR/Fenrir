namespace Fenrir

type Ref = {
    Name: string
    CommitObjectId: string
}

module Refs =
    open System.IO

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
