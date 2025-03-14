// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui

open System
open System.Collections.Generic

open Binding.Observables

open Fenrir.Git.Metadata
open Fenrir.Ui.Framework
open Fenrir.Ui.Models


type CommitsViewModel(repository: GitRepositoryModel, refs: RefsViewModel) =
    inherit LoadableViewModelBase()

    let formatCommit (commit: CommitBody) =
        commit.Rest
            |> Seq.tryItem (commit.Rest.Length - 2)
            |> Option.filter (not << String.IsNullOrWhiteSpace)
            |> Option.defaultValue "[NO MESSAGE]"
        // TODO[#88]: Properly gather commit messages

    let mutable commits: IReadOnlyList<CommitBody> = upcast Array.empty
    let mutable selectedCommitIndex: Nullable<int32> = Unchecked.defaultof<_>

    member val CommitMessages = ObservableList<string>(ResizeArray())

    member _.SelectedCommitIndex with get(): Nullable<int32> = selectedCommitIndex
    member this.SelectedCommitIndex with set(value: Nullable<int32>) =
        selectedCommitIndex <- value
        this.OnPropertyChanged()
        this.OnPropertyChanged(nameof this.SelectedCommit)

    member _.SelectedCommit: CommitBody option =
        if not selectedCommitIndex.HasValue
            || selectedCommitIndex.Value < 0
            || selectedCommitIndex.Value > commits.Count
        then None
        else Some commits.[selectedCommitIndex.Value]

    override this.Initialize() =
        PropertyUtil.onPropertyChange refs (nameof refs.SelectedRef) (fun () ->
            // TODO[#89]: Cancel previous request if it wasn't applied yet
            this.IsLoading <- true
            match refs.SelectedRef with
            | None ->
                commits <- Array.empty
                this.CommitMessages.Clear()
                this.IsLoading <- false
            | Some ref ->
                Async.runTask(async {
                    let! newCommits = repository.ReadCommitsAsync ref
                    commits <- newCommits
                    do! Async.SwitchToContext ConsoleFrameworkSynchronizationContext.instance
                    this.CommitMessages.Clear()
                    commits |> Seq.iter(formatCommit >> this.CommitMessages.Add)
                    this.IsLoading <- false
                })
        )
