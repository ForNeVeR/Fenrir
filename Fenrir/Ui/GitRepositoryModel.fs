namespace Fenrir.Ui

open Fenrir

type GitRepositoryModel(gitDirectoryPath: string) =
    member _.ReadRefsAsync(): Async<string seq> = async {
        return Refs.readRefs gitDirectoryPath
               |> Seq.toArray
               |> Seq.map(fun ref -> ref.Name)
    }
