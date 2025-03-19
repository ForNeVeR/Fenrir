// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui.Models

open System
open System.Collections.Generic

open System.Threading.Tasks
open Fenrir.Git
open Fenrir.Git.Commands
open Fenrir.Git.Metadata
open TruePath

type GitRepositoryModel(gitDirectoryPath: LocalPath) =
    let rec readTreeRecursively(hash: string) = task {
        let index = PackIndex gitDirectoryPath
        let! body = ParseTreeBody index gitDirectoryPath hash
        let! models =
            body
            |> Array.map (fun item -> task {
                let objectHash = Tools.byteToString item.Hash
                let! header = ReadObjectHeader index gitDirectoryPath objectHash

                if header.Type = GitObjectType.GitBlob
                then return Array.singleton { RootedPath = item.Name }
                else
                    let! items = readTreeRecursively objectHash
                    return items |> Array.map (fun subItem ->
                        { RootedPath = String.Format("{0}/{1}", item.Name, subItem.RootedPath) }
                    )
            })
            |> Task.WhenAll
        return models |> Array.collect id
    }

    member _.ReadRefsAsync(): Async<IReadOnlyList<Ref>> = async {
        return upcast (Refs.readRefs gitDirectoryPath
                       |> Seq.toArray)
    }

    member _.ReadCommitsAsync(ref: Ref): Async<IReadOnlyList<Commit>> = async {
        let commits = ResizeArray()
        let mutable currentCommitId = Some ref.CommitObjectId
        let index = PackIndex gitDirectoryPath
        while Option.isSome currentCommitId do
            let! commit = Async.AwaitTask <| Commits.ReadCommit(index, gitDirectoryPath, currentCommitId.Value)
            commits.Add commit
            currentCommitId <- Array.tryHead commit.Body.Parents
        return commits
    }

    member _.ReadFilesAsync(commit: Commit): Async<IReadOnlyList<TreeItemModel>> = async {
        let! result = Async.AwaitTask <| readTreeRecursively commit.Body.Tree
        return result
    }
