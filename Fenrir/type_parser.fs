open System
open System.IO
open System.Text.RegularExpressions

type GitObjectType =
    | GitCommit = 0
    | GitTree = 1
    | GitBlob = 2

let (|TypeRegex|_|) regex str = 
    let m = Regex(regex).Match(str)
    if m.Success
    then Some m.Groups.[1].Value
    else None

type GitObjectOpen(path:string) =
    member x.TypeChecker() =
        use stream = new StreamReader(path)
             
        let line = stream.ReadLine()

        match line with
            | TypeRegex "commit (\d{1,})\0" _ -> Some GitObjectType.GitCommit
            | TypeRegex "tree (\d{1,})\0" _   -> Some GitObjectType.GitTree
            | TypeRegex "blob (\d{1,})\0" _   -> Some GitObjectType.GitBlob
            | _                               -> None

[<EntryPoint>]
let main argv =
    Console.WriteLine("Print path to unpacked git object file:")
    let line = Console.ReadLine()
    let obj = GitObjectOpen(line)
    Console.WriteLine ("The type of the object:")
    match obj.TypeChecker() with 
            | Some x -> Console.WriteLine x
            | None   -> Console.WriteLine "ERROR: probably, it's not git object file" 
    0 // return an integer exit code
