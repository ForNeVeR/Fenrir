module Fenrir.Program

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

  brlist <path to repository>
    Shows branch list of repository.

  (help | --help)
    Print this message.

  object-type [<path>]
    Prints the type of the Git raw object read from the file system.

    If <path> isn't passed, then accepts raw object contents from the standard input.

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

    | [|"brlist"; pathToRepo|] ->
        let ff = Commands.readBranchList(pathToRepo)
        let ss = Array.collect (fun (cp:(String*String)) -> [|((fst cp).Substring(pathToRepo.Length + 5) + " " + (snd cp))|]) ff
        printfn "%A" ss
        ExitCodes.Success

    | [|"guillotine"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = new MemoryStream()
        Commands.unpackObject input decodedInput
        decodedInput.Position <- 0L
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        let n = Commands.guillotineObject decodedInput output
        printfn "%A bytes have been written" n
        ExitCodes.Success

    | [|"help"|] | [|"--help"|] | [||] ->
        printUsage()
        ExitCodes.Success

    | [|"object-type"|] ->
        use input = Console.OpenStandardInput()
        let header = Commands.readHeader input
        printfn "%A" header
        ExitCodes.Success
    | [|"object-type"; inputFilePath|] ->
        use input = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
        let header = Commands.readHeader input
        printfn "%A" header
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
