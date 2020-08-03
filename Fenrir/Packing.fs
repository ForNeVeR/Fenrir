module Fenrir.Packing

open System
open System.IO
open Fenrir.Zlib

let getPackPath (gitPath: String) (packFile: String) (extension: String) : String =
    Path.Combine(gitPath, "objects", "pack", packFile + extension)

let flagToBits(key: String) : byte[] =
    match key with
        | "commit" -> [| 0uy; 0uy; 1uy |]
        | "tree"   -> [| 0uy; 1uy; 0uy |]
        | "blob"   -> [| 0uy; 1uy; 1uy |]
        | "tag"    -> [| 1uy; 0uy; 0uy |]
        | _        -> failwithf "Unknown object type provided"

let anotherEndian (reader: BinaryReader) : int =
    reader.ReadBytes 4
        |> Array.map (fun x -> int x)
        |> Array.reduce (fun x y ->
            x * 256 + y)

let readHash (reader: BinaryReader) : String =
    reader.ReadBytes 20
        |> Array.fold (fun acc elem ->
            String.Concat(acc, sprintf "%02x" elem)) ""

let byteToBits (num: byte) : byte[] =
    let mutable number = num
    Array.init 8 (fun _ ->
        let result = number % 2uy
        number <- number >>> 1
        result)


let bitsToInt (bits: byte[]) =
    let mutable st = 1

    Array.foldBack (fun i acc ->
        let result = acc + int i * st
        st <- st * 2
        result) bits 0

let parseIndexOffset (path: String) (hash: String) : int =
    use idxReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    //skip header and fanout table
    idxReader.BaseStream.Position <- 1028L
    //last item in fanout table
    let size = anotherEndian idxReader
    //hashes extraction
    let hashes = Array.init size (fun _ -> readHash idxReader)
    //position binary search of the hash
    let pos = Array.BinarySearch(hashes, hash)
    //skipping crc table and getting offset location
    if pos >= 0 then
        idxReader.BaseStream.Position <-
            idxReader.BaseStream.Position + int64 size * 4L
            + (int64 pos) * 4L

        let result = anotherEndian idxReader
        idxReader.Close()
        result
    else
        idxReader.Close()
        -1

let parsePackInfo (path: String) (offset: int) (flag: String) : MemoryStream =
    let mutable flagByte = 0uy
    let bitsExtractor (reader: BinaryReader) =
        flagByte <- reader.ReadByte()
        flagByte |> byteToBits |> Array.rev

    let packReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    packReader.BaseStream.Position <- int64 offset
    let mutable bits = bitsExtractor packReader
    let mutable size = bits.[4..7] |> bitsToInt
    let mutable sizeOffset = 4

    //TODO implement delta objs
    if bits.[1..3] = flagToBits(flag) then
        while flagByte >= 128uy do
            flagByte <- packReader.ReadByte()
            size <- int (flagByte % 128uy) <<< sizeOffset ||| size
            sizeOffset <- sizeOffset + 7
    else
        failwithf "wrong type of file provided"
    let result = new MemoryStream(size + 20 |> packReader.ReadBytes) |> getDecodedStream
    packReader.Close()
    result

let getPackedStream (path : String) (hash : String) (flag : String) : MemoryStream =
    let packs = Directory.GetFiles(Path.Combine(path, "objects", "pack"), "*.idx")
                |> Array.map Path.GetFileName
                |> Array.map ((fun count (str : String) -> str.[0..str.Length - count - 1]) 4)
    let mutable offset = -1

    let containingPack = packs |> Array.tryFind (fun item ->
        offset <- parseIndexOffset
                  <| getPackPath path item ".idx"
                  <| hash
        offset <> -1)

    match offset with
        | -1 -> failwithf "git repo is corrupted"
        | _ -> parsePackInfo
               <| getPackPath path (Option.toObj containingPack) ".pack"
               <| offset
               <| flag
