namespace Fenrir.Ui

open Binding.Observables

open Fenrir.Ui.Framework

type RefsViewModel(repository: GitRepositoryModel) as this =
    inherit ViewModelBase()

    let refList = ObservableList<string>(ResizeArray())

    let mutable isLoading = true
    member _.IsLoading with get() = isLoading
    member this.IsLoading with set(value) =
        isLoading <- value
        this.OnPropertyChanged()

    member _.RefList: ObservableList<string> =
        refList

    override _.Initialize() =
        Async.runTask(async {
            let! refs = repository.ReadRefsAsync()
            do! Async.SwitchToContext ConsoleFrameworkSynchronizationContext.instance
            refs |> Seq.iter refList.Add
            this.IsLoading <- false
        })
