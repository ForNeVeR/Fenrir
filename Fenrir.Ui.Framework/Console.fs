module Fenrir.Ui.Framework.Console

open System

open Fenrir.Ui.Framework.Native

let private initializeWindows() =
    let stdin = Windows.getStdInHandle()
    let consoleMode = Windows.getConsoleMode stdin
    if consoleMode &&& Windows.Constants.ENABLE_QUICK_EDIT_MODE <> 0u then
        let newMode =
            (consoleMode ||| Windows.Constants.ENABLE_EXTENDED_FLAGS) &&& ~~~Windows.Constants.ENABLE_QUICK_EDIT_MODE
        Windows.setConsoleMode stdin newMode

let initialize() =
    match Environment.OSVersion.Platform with
    | PlatformID.Win32NT -> initializeWindows()
    | _ -> ()
