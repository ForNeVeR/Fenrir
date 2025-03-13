// SPDX-FileCopyrightText: 2020-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Fenrir.Ui.Framework.ConsoleFrameworkSynchronizationContext

open System.Threading

open ConsoleFramework

let instance: SynchronizationContext =
    { new SynchronizationContext() with
         member this.Post(d: SendOrPostCallback, state: obj): unit =
             ConsoleApplication.Instance.Post(fun _ -> d.Invoke state)
         member _.Send(d: SendOrPostCallback, state: obj): unit =
             ConsoleApplication.Instance.RunOnUiThread(fun _ -> d.Invoke state)
    }
