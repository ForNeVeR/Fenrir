﻿namespace Fenrir.Ui

open System
open System.ComponentModel

open Binding.Observables

open Fenrir
open Fenrir.Ui.Framework

type CommitsViewModel(repository: GitRepositoryModel, refs: RefsViewModel) =
    inherit ViewModelBase()

    let formatCommit (commit: Commands.CommitBody) =
        commit.Rest
        |> Seq.skip 2
        |> Seq.filter(fun s -> not(s.StartsWith "gpgsig"))
        |> Seq.tryFind(not << String.IsNullOrWhiteSpace)
        |> Option.defaultValue "[NO MESSAGE]"
        // TODO: Properly gather commit messages

    let commitList = ObservableList<string>(ResizeArray())

    let mutable isLoading = true
    member _.IsLoading with get() = isLoading
    member this.IsLoading with set(value) =
        isLoading <- value
        this.OnPropertyChanged()

    member _.CommitList: ObservableList<string> =
        commitList

    override this.Initialize() =
        (refs :> INotifyPropertyChanged).PropertyChanged.Add(fun event ->
            if event.PropertyName = "SelectedRef" then
                // TODO: Cancel previous request if it wasn't applied yet
                this.IsLoading <- true
                match refs.SelectedRef with
                | None ->
                    commitList.Clear()
                    this.IsLoading <- false
                | Some ref ->
                    Async.runTask(async {
                        let! commits = repository.ReadCommitsAsync ref
                        do! Async.SwitchToContext ConsoleFrameworkSynchronizationContext.instance
                        commitList.Clear()
                        commits |> Seq.iter(formatCommit >> commitList.Add)
                        this.IsLoading <- false
                    })
        )
