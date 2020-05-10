namespace Fenrir.Ui

open Fenrir

type GitRepositoryModel(gitDirectoryPath: string) =
    member _.ReadRefsAsync(): Async<string seq> = async {
        return Commands.readBranchList gitDirectoryPath
               |> Seq.map fst
    }
