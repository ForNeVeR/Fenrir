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

  brlist [<path to .git/ directory>]
    Shows branch list of repository.

    If <path to .git/ directory> isn't passed, then current directory are used instead.

  guillotine [<input> [<output>]]
    Read git file and write decoded content of the file without header.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  (help | --help)
    Print this message.

  object-type [<path>]
    Prints the type of the Git raw object read from the file system.

    If <path> isn't passed, then accepts raw object contents from the standard input.

  pack [<input> [<output>]]
    Reads the object file passed as <input> and packs the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  save [<input> [<path to repository>]]
    Read text file and save it as object file to repository.

    If <path to repository> isn't passed, then current directory are used instead.
    If the <input> isn't defined, then standard input are used instead.

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

    | [|"brlist"|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        let ff = Commands.readBranchList(pathToRepo)
        let ss = Array.collect (fun (cp:(String*String)) -> [|((fst cp).Substring(pathToRepo.Length + 5) + " " + (snd cp))|]) ff
        printfn "%A" ss
        ExitCodes.Success
    | [|"brlist"; pathToRepo|] ->
        let ff = Commands.readBranchList(pathToRepo)
        let ss = Array.collect (fun (cp:(String*String)) -> [|((fst cp).Substring(pathToRepo.Length + 5) + " " + (snd cp))|]) ff
        printfn "%A" ss
        ExitCodes.Success

    | [|"guillotine"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        use decodedInput = new MemoryStream()
        Commands.unpackObject input decodedInput
        decodedInput.Position <- 0L
        let n = Commands.guillotineObject decodedInput output
        printfn "%A bytes have been written" n
        ExitCodes.Success
    | [|"guillotine"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = new MemoryStream()
        Commands.unpackObject input decodedInput
        decodedInput.Position <- 0L
        use output = Console.OpenStandardOutput()
        let n = Commands.guillotineObject decodedInput output
        printfn "%A bytes have been written" n
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

    | [|"pack"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        Commands.packObject input output
        ExitCodes.Success
    | [|"pack"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = Console.OpenStandardOutput()
        Commands.packObject input output
        ExitCodes.Success
    | [|"pack"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        Commands.packObject input output
        ExitCodes.Success

    | [|"save"|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        use input = Console.OpenStandardInput()
        use headed = new MemoryStream()
        Commands.hydraBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed |> Commands.byteToString
        headed.Position <- 0L
        let pathToFile = Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2), hashName.Substring(2, 38))
        Directory.CreateDirectory(Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2))) |> ignore
        use output = new FileStream(pathToFile, FileMode.CreateNew, FileAccess.Write)
        Commands.packObject headed output
        ExitCodes.Success
    | [|"save"; inputPath|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        Commands.hydraBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed |> Commands.byteToString
        headed.Position <- 0L
        let pathToFile = Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2), hashName.Substring(2, 38))
        Directory.CreateDirectory(Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2))) |> ignore
        use output = new FileStream(pathToFile, FileMode.CreateNew, FileAccess.Write)
        Commands.packObject headed output
        ExitCodes.Success
    | [|"save"; inputPath; pathToRepo|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        Commands.hydraBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed |> Commands.byteToString
        headed.Position <- 0L
        let pathToFile = Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2), hashName.Substring(2, 38))
        Directory.CreateDirectory(Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2))) |> ignore
        use output = new FileStream(pathToFile, FileMode.CreateNew, FileAccess.Write)
        Commands.packObject headed output
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
