namespace Fenrir.Ui

open System
open System.Collections.Generic

open Binding.Observables

open Fenrir
open Fenrir.Ui.Framework

type RefsViewModel(repository: GitRepositoryModel) as this =
    inherit LoadableViewModelBase()

    let mutable refs: IReadOnlyList<Ref> = upcast Array.empty
    let commitList = ObservableList<string>(ResizeArray())
    let mutable selectedRefIndex: Nullable<int32> = Unchecked.defaultof<_>

    member _.RefList: ObservableList<string> =
        commitList

    member _.SelectedRefIndex with get(): Nullable<int32> = selectedRefIndex
    member this.SelectedRefIndex with set(value: Nullable<int32>) =
        selectedRefIndex <- value
        this.OnPropertyChanged()
        this.OnPropertyChanged("SelectedRef")

    member _.SelectedRef: Ref option =
        if not selectedRefIndex.HasValue || selectedRefIndex.Value < 0 || selectedRefIndex.Value > refs.Count
        then None
        else Some refs.[selectedRefIndex.Value]

    override _.Initialize() =
        Async.runTask(async {
            let! newRefs = repository.ReadRefsAsync()
            do! Async.switchToUiThread()
            refs <- newRefs
            refs |> Seq.iter (fun ref -> commitList.Add ref.Name)
            this.IsLoading <- false
        })
