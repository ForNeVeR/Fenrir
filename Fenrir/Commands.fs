﻿module Fenrir.Commands

open System
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

type ObjectHeader = {
    Type: GitObjectType
    Size: UInt64
}

let readWhile (condition: byte -> bool) (maxSize: uint64) (stream: BinaryReader): byte array =
    let rec makeList (n: uint64): byte list =
        let newByte = stream.ReadByte()
        match (condition newByte) with
            | _ when n > (maxSize) || stream.PeekChar() = -1 -> failwithf "Invalid Git object header"
            | true  -> newByte :: makeList (n + 1UL)
            | false -> []
    makeList(0UL) |> List.toArray

let readHeader(input: Stream): ObjectHeader =
    let bF = new BinaryReader(input)

    let maxTypeNameLength = uint64 "commit".Length
    let typeArray = readWhile (fun b -> not (b.Equals(32uy))) maxTypeNameLength bF
    let tp =
        match typeArray with
        | "tree"B   -> GitObjectType.GitTree
        | "commit"B -> GitObjectType.GitCommit
        | "blob"B   -> GitObjectType.GitBlob
        | _         -> failwithf "Invalid Git object header"

    let maxLength = uint64 (string UInt64.MaxValue).Length
    let sizeArray = readWhile (fun b -> not <| (b.Equals 0uy)) maxLength bF
    let sz = Convert.ToUInt64(System.Text.Encoding.UTF8.GetString(sizeArray))

    {Type = tp; Size = sz}
