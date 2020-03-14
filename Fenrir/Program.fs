module Fenrir.Program

open System
open System.IO
open System.Reflection

let private printVersion() =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn "v%A" version

let private printUsage() =
    printf @"
Usage:

  (help | --help)
    Print this message.

  unpack [<input> [<output>]]
    Unpacks the object file passed as <input> and writes the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  (version | --version)
    Print the program version.
"

let private printUnrecognizedArguments argv =
    printfn "Arguments were not recognized: %A" argv

module ExitCodes =
    let Success = 0
    let UnrecognizedArguments = 1

[<EntryPoint>]
let main (argv: string[]): int =
    match argv with
    | [|"help"|] | [|"--help"|] | [||] ->
        printUsage()
        ExitCodes.Success

    | [|"unpack"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        Commands.unpackObject input output
        ExitCodes.Success
    | [|"unpack"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = Console.OpenStandardOutput()
        Commands.unpackObject input output
        ExitCodes.Success
    | [|"unpack"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        Commands.unpackObject input output
        ExitCodes.Success

    | [|"version"|] | [|"--version"|] ->
        printVersion()
        ExitCodes.Success

    | _ ->
        printUnrecognizedArguments argv
        ExitCodes.UnrecognizedArguments
