namespace Fenrir.Ui

open System.Collections.Generic

open Fenrir

type GitRepositoryModel(gitDirectoryPath: string) =
    member _.ReadRefsAsync(): Async<IReadOnlyList<Ref>> = async {
        return upcast (Refs.readRefs gitDirectoryPath
                       |> Seq.toArray)
    }

    member _.ReadCommitsAsync(ref: Ref): Async<IReadOnlyList<Commands.CommitBody>> = async {
        return upcast [|
            let mutable currentCommitId = Some ref.CommitObjectId
            while Option.isSome currentCommitId do
                let body = Commands.parseCommitBody gitDirectoryPath currentCommitId.Value
                yield body
                currentCommitId <- Array.tryHead body.Parents
        |]
    }
