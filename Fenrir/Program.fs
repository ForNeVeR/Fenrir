open System
open System.Reflection

let private printVersion() =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn "v%A" version

let private printUsage() =
    printf @"
Usage:

  version
  --version
    Print the program version.

  help
  --help
    Print this message.
"

let private printUnrecognizedArguments argv =
    printfn "Arguments were not recognized: %A" argv

module ExitCodes =
    let Success = 0
    let UnrecognizedArguments = 1

[<EntryPoint>]
let main (argv: string[]): int =
    match argv with
    | [|"version"|] | [|"--version"|] ->
        printVersion()
        ExitCodes.Success
    | [|"help"|] | [|"--help"|] | [||] ->
        printUsage()
        ExitCodes.Success
    | _ ->
        printUnrecognizedArguments argv
        ExitCodes.UnrecognizedArguments
