module Fenrir.Commands

open System
open System.Globalization
open System.IO

open ICSharpCode.SharpZipLib.Zip.Compression.Streams

let unpackObject (input: Stream) (output: Stream): unit =
    use deflate = new InflaterInputStream(input)
    deflate.CopyTo output

type GitObjectType =
    | GitCommit = 0
    | GitTree   = 1
    | GitBlob   = 2

type ObjectHeader = {
    Type: GitObjectType
    Size: UInt64
}

type CommitBody = {
    Tree : String
    Parents : String[]
    Rest : String[]
}

let readWhile (condition: byte -> bool) (maxSize: uint64) (stream: BinaryReader): byte array =
    let rec makeList (n: uint64): byte list =
        let newByte = stream.ReadByte()
        match (condition newByte) with
            | _ when n > (maxSize) -> failwithf "Invalid Git object header"
            | true                 -> newByte :: makeList (n + 1UL)
            | false                -> []
    makeList(0UL) |> List.toArray

let readHeader(input: Stream): ObjectHeader =
    let bF = new BinaryReader(input)

    let maxTypeNameLength = uint64 "commit".Length
    let typeArray = readWhile (fun b -> b <> byte ' ') maxTypeNameLength bF
    let tp =
        match typeArray with
        | "tree"B   -> GitObjectType.GitTree
        | "commit"B -> GitObjectType.GitCommit
        | "blob"B   -> GitObjectType.GitBlob
        | _         -> failwithf "Invalid Git object header"

    let maxLength = uint64 (string UInt64.MaxValue).Length
    let sizeArray = readWhile (fun b -> b <> 0uy) maxLength bF
    let sz = Convert.ToUInt64(System.Text.Encoding.ASCII.GetString(sizeArray), CultureInfo.InvariantCulture)

    {Type = tp; Size = sz}

let guillotineObject (input: Stream) (output: Stream): int =
    readHeader input |> ignore
    let bR = new BinaryReader(input)
    let bW = new BinaryWriter(output)
    let rec rewrite n:int =
        try
            bW.Write(bR.ReadByte())
            rewrite (n + 1)
        with
            | :? EndOfStreamException -> n
    rewrite 0

let readBranchList (path : String): (String*String)[] =
    let sf = Directory.GetFiles (path + ".git/refs/heads/")
    Array.collect (fun (cp:String) -> [|(cp, File.ReadAllLines(cp).[0])|]) sf

let parseCommitBody (path : String) (hash : String) : CommitBody =
    let pathToFile = path + "/.git/objects/" + hash.Substring(0, 2) + "/" + hash.Substring(2, 38)
    use input = new FileStream(pathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
    use decodedInput = new MemoryStream()
    unpackObject input decodedInput
    decodedInput.Position <- 0L
    match (readHeader decodedInput).Type with
        | GitObjectType.GitTree   -> failwithf "Found tree file instead of commit file"
        | GitObjectType.GitBlob   -> failwithf "Found blob file instead of commit file"
        | GitObjectType.GitCommit ->
            let enc = System.Text.Encoding.UTF8
            use sr = new StreamReader(decodedInput, enc)
            let tree = sr.ReadLine().Substring(5)
            let rec parseParents (s : StreamReader) (P : String list) : (String list * String[]) =
                let str = s.ReadLine()
                match str.Substring(0, 7) with
                    | "parent " -> parseParents s (str.Substring(7, 40) :: P)
                    | _         -> (P, [|str|])
            let (p, r) = parseParents sr []
            let rr = (sr.ReadToEnd()).Split "\n" |> Array.append r
            {Tree = tree; Parents = (Array.ofList p); Rest = rr}
