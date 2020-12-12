namespace Fenrir.Ui.Framework

type LoadableViewModelBase() =
    inherit ViewModelBase()

    let mutable isLoading = true
    member _.IsLoading with get() = isLoading
    member this.IsLoading with set(value) =
        isLoading <- value
        this.OnPropertyChanged()
