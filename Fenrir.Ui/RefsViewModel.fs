namespace Fenrir.Ui

open Binding.Observables

open Fenrir.Ui.Framework

type RefsViewModel(path: string) as this =
    inherit ViewModelBase()

    let refs = ObservableList<string>(ResizeArray())

    do refs.Add "fff"
    do refs.Add "ddd"

    do ignore <| Async.StartAsTask(async {
        do! Async.Sleep 5000
        this.IsLoading <- false
    })

    let mutable isLoading = true
    member _.IsLoading with get() = isLoading
    member this.IsLoading with set(value) =
        isLoading <- value
        this.OnPropertyChanged()

    member _.RefList: ObservableList<string> =
        refs
