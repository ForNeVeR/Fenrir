// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui.Models

open System
open System.Collections.Generic

open Fenrir.Git
open Fenrir.Git.Commands
open Fenrir.Git.Metadata
open TruePath

type GitRepositoryModel(gitDirectoryPath: string) =
    let rec readTreeRecursively(hash: string) =
        parseTreeBody gitDirectoryPath hash
        |> Array.collect (fun item ->
            let objectHash = Tools.byteToString item.Hash
            let header = readObjectHeader gitDirectoryPath objectHash

            if header.Type = GitObjectType.GitBlob
            then Array.singleton { RootedPath = item.Name }
            else
                readTreeRecursively objectHash
                |> Array.map (fun subItem ->
                    { RootedPath = String.Format("{0}/{1}", item.Name, subItem.RootedPath) }
                )
        )

    member _.ReadRefsAsync(): Async<IReadOnlyList<Ref>> = async {
        return upcast (Refs.readRefs gitDirectoryPath
                       |> Seq.toArray)
    }

    member _.ReadCommitsAsync(ref: Ref): Async<IReadOnlyList<Commit>> = async {
        let commits = ResizeArray()
        let mutable currentCommitId = Some ref.CommitObjectId
        while Option.isSome currentCommitId do
            let! commit = Async.AwaitTask <| Commits.ReadCommit(LocalPath gitDirectoryPath, currentCommitId.Value)
            commits.Add commit
            currentCommitId <- Array.tryHead commit.Body.Parents
        return commits
    }

    member _.ReadFilesAsync(commit: Commit): Async<IReadOnlyList<TreeItemModel>> = async {
        return upcast readTreeRecursively commit.Body.Tree
    }
