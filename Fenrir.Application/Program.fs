module Fenrir.Program

open System
open System.IO
open System.Reflection

open Fenrir
open Fenrir.ArgumentCommands
open Fenrir.Metadata

let private printVersion() =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn "v%A" version

let private printUsage() =
    printf @"
Usage:

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

  refs [<path to .git/ directory>]
    Shows branch list of repository.

  init [<path>]
    Create an empty Git repository or reinitialize an existing one

    If <path to .git/ directory> isn't passed, then current directory are used instead.

  save [<input> [<path to repository>]]
    Read text file and save it as object file to repository.

    If <path to repository> isn't passed, then current directory are used instead.
    If the <input> isn't defined, then standard input are used instead.

  ui [<path>]
    Shows UI to select a commit from the repository identified by <path> (.git subdirectory of current directory by default).

  unpack [<input> [<output>]]
    Unpacks the object file passed as <input> and writes the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  update-commit <id of commit> [<path to repository>] <path of file>
    Read updated text file from <path to repository>/<path of file> and save it as new object file to repository.
    Moreover, save new trees based on <id of commit> parent tree, its subtrees to repository and commit file.

    If <path to repository> isn't passed, then current directory are used instead.

  update-with-trees <id of root tree> [<path to repository>] <path of file>
    Read updated text file from <path to repository>/<path of file> and save it as new object file to repository.
    Moreover, save new trees based on <id of root tree> tree and its subtrees to repository.

    If <path to repository> isn't passed, then current directory are used instead.

  (version | --version)
    Print the program version.
"

[<EntryPoint>]
let main (argv: string[]): int =
    match argv with
    | [|"guillotine"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        use decodedInput = new MemoryStream()
        Zlib.unpackObject input decodedInput
        decodedInput.Position <- 0L
        let n = Commands.guillotineObject decodedInput output
        printfn "%A bytes have been written" n
        ExitCodes.Success
    | [|"guillotine"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = new MemoryStream()
        Zlib.unpackObject input decodedInput
        decodedInput.Position <- 0L
        use output = Console.OpenStandardOutput()
        let n = Commands.guillotineObject decodedInput output
        printfn "%A bytes have been written" n
        ExitCodes.Success
    | [|"guillotine"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = new MemoryStream()
        Zlib.unpackObject input decodedInput
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
        Zlib.packObject input output
        ExitCodes.Success
    | [|"pack"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = Console.OpenStandardOutput()
        Zlib.packObject input output
        ExitCodes.Success
    | [|"pack"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        Zlib.packObject input output
        ExitCodes.Success

    | [|"refs"|] ->
        let pathToRepo = Path.Combine(Directory.GetCurrentDirectory(), ".git")
        Commands.refsCommand pathToRepo
        ExitCodes.Success
    | [|"refs"; pathToRepo|] ->
        Commands.refsCommand pathToRepo
        ExitCodes.Success

    | [|"save"|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        use input = Console.OpenStandardInput()
        use headed = new MemoryStream()
        let hashName = Commands.headifyStream GitObjectType.GitBlob input headed
        Commands.writeStreamToFile pathToRepo headed hashName
        ExitCodes.Success
    | [|"save"; inputPath|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        let hashName = Commands.headifyStream GitObjectType.GitBlob input headed
        Commands.writeStreamToFile pathToRepo headed hashName
        ExitCodes.Success
    | [|"save"; inputPath; pathToRepo|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        let hashName = Commands.headifyStream GitObjectType.GitBlob input headed
        Commands.writeStreamToFile pathToRepo headed hashName
        ExitCodes.Success

    | [|"ui"; path|] ->
        Ui.EntryPoint.run path
        ExitCodes.Success
    | [|"ui"|] ->
        Ui.EntryPoint.run(Path.Combine(Environment.CurrentDirectory, ".git"))
        ExitCodes.Success

    | [|"unpack"|] ->
        use input = Console.OpenStandardInput()
        use output = Console.OpenStandardOutput()
        Zlib.unpackObject input output
        ExitCodes.Success
    | [|"unpack"; inputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = Console.OpenStandardOutput()
        Zlib.unpackObject input output
        ExitCodes.Success
    | [|"unpack"; inputPath; outputPath|] ->
        use input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read)
        use output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write)
        Zlib.unpackObject input output
        ExitCodes.Success

    | [|"update-commit"; commitHash; filePath|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        updateCommitOp commitHash pathToRepo filePath false

    | [|"update-commit"; commitHash; filePath; repoOrForce|] ->
        match repoOrForce.Equals "--force" with
        | true ->
            let pathToRepo = Directory.GetCurrentDirectory()
            updateCommitOp commitHash pathToRepo filePath true
        | false ->
            updateCommitOp commitHash repoOrForce filePath false

    | [|"update-commit"; commitHash; filePath; pathToRepo; forceKey|] ->
        match forceKey.Equals "--force" with
        | true -> updateCommitOp commitHash pathToRepo filePath true
        | false -> unrecognizedArgs(argv)

    | [|"update-with-trees"; rootTreeHash; filePath|] ->
        let pathToRepo = Directory.GetCurrentDirectory()
        let pathToDotGit = Path.Combine(pathToRepo, ".git")
        let fullPathToFile = Path.Combine(pathToRepo, filePath)
        use input = new FileStream(fullPathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        Commands.writeObjectHeader GitObjectType.GitBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed |> Tools.byteToString
        headed.Position <- 0L
        let pathToBlob = Commands.getRawObjectPath (Path.Combine(pathToRepo, ".git")) hashName
        Directory.CreateDirectory(Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2))) |> ignore
        use output = new FileStream(pathToBlob, FileMode.CreateNew, FileAccess.Write)
        Zlib.packObject headed output
        Commands.updateObjectInTree rootTreeHash pathToDotGit filePath hashName |> Commands.writeTreeObjects pathToRepo
        ExitCodes.Success
    | [|"update-with-trees"; rootTreeHash; pathToRepo; filePath|] ->
        let pathToDotGit = Path.Combine(pathToRepo, ".git")
        let fullPathToFile = Path.Combine(pathToRepo, filePath)
        use input = new FileStream(fullPathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
        use headed = new MemoryStream()
        Commands.writeObjectHeader GitObjectType.GitBlob input headed
        input.CopyTo headed
        headed.Position <- 0L
        let hashName = Commands.SHA1 headed |> Tools.byteToString
        headed.Position <- 0L
        let pathToBlob = Commands.getRawObjectPath (Path.Combine(pathToRepo, ".git")) hashName
        Directory.CreateDirectory(Path.Combine(pathToRepo, ".git", "objects", hashName.Substring(0, 2))) |> ignore
        use output = new FileStream(pathToBlob, FileMode.CreateNew, FileAccess.Write)
        Zlib.packObject headed output
        Commands.updateObjectInTree rootTreeHash pathToDotGit filePath hashName |> Commands.writeTreeObjects pathToRepo
        ExitCodes.Success

    | [|"verify-pack"; packPath|] ->
        let packFileName = Path.ChangeExtension(packPath, ".pack")
        if (not (File.Exists(packFileName))) then
            printfn $"{packFileName} file not found"
            ExitCodes.UnrecognizedArguments
        else
            Commands.verifyPack packFileName false
            |> Seq.toArray
            |> fun a -> String.Join(Environment.NewLine, a)
            |> Console.WriteLine

            ExitCodes.Success
    | [|"verify-pack"; packPath; mode|] ->
        let packFileName = Path.ChangeExtension(packPath, ".pack")
        if (not (File.Exists(packFileName))) then
            printfn $"{packFileName} file not found"
            ExitCodes.UnrecognizedArguments
        else
            Commands.verifyPack packFileName (mode = "-v")
            |> Seq.toArray
            |> fun a -> String.Join(Environment.NewLine, a)
            |> Console.WriteLine

            ExitCodes.Success

    | [|"version"|] | [|"--version"|] ->
        printVersion()
        ExitCodes.Success

    | [|"init"|] ->
        let currentDir = Directory.GetCurrentDirectory()
        Commands.createEmptyRepo currentDir
        ExitCodes.Success

    | _ ->
        unrecognizedArgs(argv)
