// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui.Models

open System
open System.Collections.Generic

open Fenrir
open Fenrir.Commands
open Fenrir.Metadata

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

    member _.ReadCommitsAsync(ref: Ref): Async<IReadOnlyList<CommitBody>> = async {
        return upcast [|
            let mutable currentCommitId = Some ref.CommitObjectId
            while Option.isSome currentCommitId do
                let body = Commands.parseCommitBody gitDirectoryPath currentCommitId.Value
                yield body
                currentCommitId <- Array.tryHead body.Parents
        |]
    }

    member _.ReadFilesAsync(commit: CommitBody): Async<IReadOnlyList<TreeItemModel>> = async {
        return upcast readTreeRecursively commit.Tree
    }
