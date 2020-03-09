open System
open System.IO
open System.Reflection

open Fenrir

let private printVersion() =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn "v%A" version

let private printUsage() =
    printf @"
Usage:

  object-type [<path>]
    Prints the type of the Git raw object read from the file system.

    If <path> isn't passed, then accepts raw object contents from the standard input.

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
    | [|"object-type"|] ->
        use input = Console.OpenStandardInput()
        Commands.printObjectPath input
        ExitCodes.Success
    | [|"object-type"; inputFilePath|] ->
        use input = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
        Commands.printObjectPath input
        ExitCodes.Success

    | [|"version"|] | [|"--version"|] ->
        printVersion()
        ExitCodes.Success
    | [|"help"|] | [|"--help"|] | [||] ->
        printUsage()
        ExitCodes.Success
    | _ ->
        printUnrecognizedArguments argv
        ExitCodes.UnrecognizedArguments
