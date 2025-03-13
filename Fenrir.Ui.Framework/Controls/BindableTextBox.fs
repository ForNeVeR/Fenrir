// SPDX-FileCopyrightText: 2020-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui.Framework.Controls

open ConsoleFramework.Controls

type BindableListBox() as this =
    inherit ListBox()

    do this.SelectedItemIndexChanged.Add(fun _ ->
        this.DoRaisePropertyChanged("SelectedItemIndex"))

    member private this.DoRaisePropertyChanged(propertyName: string) =
        this.RaisePropertyChanged propertyName
