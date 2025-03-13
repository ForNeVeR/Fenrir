// SPDX-FileCopyrightText: 2020-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui.Framework

open System.ComponentModel
open System.Runtime.CompilerServices

[<AbstractClass>]
type ViewModelBase() =
    let propertyChangedEvent = Event<_, _>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish

    member this.OnPropertyChanged([<CallerMemberName>] ?name: string): unit =
        match name with
        | Some name -> propertyChangedEvent.Trigger(this, PropertyChangedEventArgs(name))
        | None -> failwithf "Invalid OnPropertyChanged call"

    abstract Initialize: unit -> unit
    default _.Initialize() = ()
