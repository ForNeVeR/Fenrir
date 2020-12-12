module Fenrir.Packing

open System
open System.Collections
open System.IO

open Fenrir.Zlib
open Fenrir.DeltaCommands
open Fenrir.Tools

let getPackPath (gitPath: String) (packFile: String) (extension: String) : String =
    Path.Combine(gitPath, "objects", "pack", packFile + extension)

// https://git-scm.com/docs/pack-format
type private PackedObjectType =
    | Commit = 1
    | Tree = 2
    | Blob = 3
    | Tag = 4
    | OfsDelta = 6
    | RefDelta = 7

let getBigEndian (reader: BinaryReader) : int =
    reader.ReadInt32() |> Net.IPAddress.NetworkToHostOrder

let readHash (reader: BinaryReader) : String =
    reader.ReadBytes 20
        |> Array.fold (fun acc elem ->
            String.Concat(acc, sprintf "%02x" elem)) ""

let parseIndexOffset (path: String) (hash: String) : int =
    use idxReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    //skip header and fanout table
    idxReader.BaseStream.Position <- 1028L
    //last item in fanout table
    let size = getBigEndian idxReader
    //hashes extraction
    let hashes = Array.init size (fun _ -> readHash idxReader)
    //position binary search of the hash
    let pos = Array.BinarySearch(hashes, hash)
    //skipping crc table and getting offset location
    if pos >= 0 then
        idxReader.BaseStream.Position <-
            idxReader.BaseStream.Position + int64 size * 4L
            + (int64 pos) * 4L

        getBigEndian idxReader
    else
        -1

let private getObjectKind(bits: BitArray): PackedObjectType =
    let array = Array.zeroCreate 1
    bits.CopyTo(array, 0)
    enum array.[0]

let rec parsePackInfo (path: String) (offset: int): MemoryStream =
    let getStream (path: String) (hash: String): MemoryStream =
        let packs = Directory.GetFiles(Path.Combine(path, "objects", "pack"), "*.idx")
                    |> Array.map Path.GetFileName
                    |> Array.map ((fun count (str: String) -> str.[0..str.Length - count - 1]) 4)
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

    use packReader = new BinaryReader(File.Open(path,
                                                FileMode.Open,
                                                FileAccess.Read,
                                                FileShare.Read))
    packReader.BaseStream.Position <- int64 offset

    let sizeBytes = readWhileLast isMsbSet (uint64 packReader.BaseStream.Length) packReader
    let bits = sizeBytes.[0] |> byteToBits
    let mutable size = sliceBitArray bits 0 3 |> bitsToInt

    size <- estimateSize size 4 sizeBytes.[1..]
    let objectMask = sliceBitArray bits 4 6
    match getObjectKind objectMask with
    | PackedObjectType.Commit | PackedObjectType.Tree | PackedObjectType.Blob | PackedObjectType.Tag ->
        new MemoryStream(size + 20 |> packReader.ReadBytes) |> getDecodedStream
    | PackedObjectType.OfsDelta ->
        let mutable additional = 0
        let mutable offStep = 0
        let negOffset = (readWhileLast isMsbSet (uint64 packReader.BaseStream.Length) packReader
                        |> Array.fold (fun acc elem ->
                            additional <- additional + (1 <<< offStep)
                            offStep <- offStep + 7
                            (acc <<< 7) ||| int (elem % 128uy)
                            ) 0
                        )
                        + additional - 1

        use stream = parsePackInfo path (offset - negOffset)
        use nonDeltaReader = new BinaryReader(stream)
        processDelta packReader nonDeltaReader size
    | PackedObjectType.RefDelta ->
        let hash = packReader |> readHash
        use stream = getStream path hash
        use nonDeltaReader = new BinaryReader(stream)
        processDelta packReader nonDeltaReader size
    | o -> failwithf "Cannot parse object type from a pack file: %A" o

let getPackedStream (path: String) (hash: String): MemoryStream =
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
        | -1 -> failwithf "pack source not found, git repo is corrupted"
        | _ -> parsePackInfo
               <| getPackPath path (Option.toObj containingPack) ".pack"
               <| offset
