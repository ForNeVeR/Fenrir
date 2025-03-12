module Fenrir.Ui.Framework.Async

open System

let runTask(task: Async<unit>): unit =
    ignore <| Async.StartAsTask(async {
        try
            do! task
        with
        | ex ->
            printfn "Exception: %A" ex
            Environment.FailFast("Unhandled async exception", ex)
    })

let switchToUiThread(): Async<unit> =
    Async.SwitchToContext ConsoleFrameworkSynchronizationContext.instance
