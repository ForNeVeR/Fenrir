// SPDX-FileCopyrightText: 2020-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Fenrir.Ui.Framework.PropertyUtil

open System.ComponentModel

let onPropertyChange (model: INotifyPropertyChanged) (propertyName: string) (handler: unit -> unit): unit =
    model.PropertyChanged.Add(fun event ->
        if event.PropertyName = propertyName then
            handler()
    )
