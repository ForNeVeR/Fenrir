module Fenrir.Commands

open System
open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression.Streams

let unpackObject (input: Stream) (output: Stream): unit =
    use deflate = new InflaterInputStream(input)
    deflate.CopyTo output

type GitObjectType =
    | GitCommit = 0
    | GitTree   = 1
    | GitBlob   = 2

type GitHeader = {tp:GitObjectType; sz:UInt64}

let checkTypeAndSize(input: Stream): GitHeader =
    let bF = new BinaryReader(input)
    let maxHeaderLength = (string UInt64.MaxValue).Length
    let rec readType(x:byte[], y:int32) =
        let newByte = bF.ReadByte()
        match x with
            | _ when y > 7 || bF.PeekChar() = -1  -> invalidArg "notGitObject" "It's not a git object"
            | [|116uy; 114uy; 101uy; 101uy|]      -> GitObjectType.GitTree
            | [|99uy; 111uy; 109uy;
                109uy; 105uy; 116uy|]             -> GitObjectType.GitCommit
            | [|98uy; 108uy; 111uy; 98uy|]        -> GitObjectType.GitBlob
            | _                                   -> readType(Array.append x [|newByte;|], y + 1)
    let rec readSize(x:byte[], y:int32) =
        let newByte = bF.ReadByte()
        match newByte with
            | _ when y > maxHeaderLength
                     || bF.PeekChar() = -1   -> invalidArg "notGitObject" "It's not a git object"
            | 0uy                            -> Convert.ToUInt64(System.Text.Encoding.UTF8.GetString(x))
            | _                              -> readSize(Array.append x [|newByte;|], y + 1)

    {tp = readType([||], 0); sz = readSize([||], 0)}
