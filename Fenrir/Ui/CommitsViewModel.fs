namespace Fenrir.Ui

open System.ComponentModel

open Binding.Observables

open Fenrir
open Fenrir.Ui.Framework

type CommitsViewModel(repository: GitRepositoryModel, refs: RefsViewModel) as this =
    inherit ViewModelBase()

    let formatCommit (commit: Commands.CommitBody) = sprintf "%A" (Array.tryHead commit.Rest)

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
                        commits |> Seq.iter(formatCommit >> commitList.Add)
                        this.IsLoading <- false
                    })
        )

