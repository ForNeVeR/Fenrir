module Fenrir.Packing

open System
open System.IO

open Fenrir.Zlib
open Fenrir.DeltaCommands
open Fenrir.Tools

let getPackPath (gitPath: String) (packFile: String) (extension: String) : String =
    Path.Combine(gitPath, "objects", "pack", packFile + extension)

let bitKeys(key: String) : byte[] =
    match key with
        | "commit" -> [| 0uy; 0uy; 1uy |]
        | "tree"   -> [| 0uy; 1uy; 0uy |]
        | "blob"   -> [| 0uy; 1uy; 1uy |]
        | "tag"    -> [| 1uy; 0uy; 0uy |]
        | _        -> failwithf "Unknown object type provided"

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

let rec parsePackInfo (path: String) (offset: int) (flag: String): MemoryStream =
    let getStream (path : String) (hash : String) (flag : String) : MemoryStream =
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

    use packReader = new BinaryReader(File.Open(path,
                                                FileMode.Open,
                                                FileAccess.Read,
                                                FileShare.Read))
    packReader.BaseStream.Position <- int64 offset

    let sizeBytes = readWhileLast isMsbSet (uint64 packReader.BaseStream.Length) packReader
    let bits = sizeBytes.[0] |> byteToBits |> Array.rev
    let mutable size = bits.[4..7] |> bitsToInt

    size <- estimateSize size 4 sizeBytes.[1..]
    if bits.[1..3] = bitKeys(flag) then
        new MemoryStream(size + 20 |> packReader.ReadBytes) |> getDecodedStream
    else if bits.[1..3] = [| 1uy; 1uy; 0uy |] then
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

        use stream = parsePackInfo path (offset - negOffset) flag
        use nonDeltaReader = new BinaryReader(stream)
        processDelta packReader nonDeltaReader size
    else if bits.[1..3] =  [| 1uy; 1uy; 1uy |] then
        let hash = packReader |> readHash
        use stream = getStream path hash flag
        use nonDeltaReader = new BinaryReader(stream)
        processDelta packReader nonDeltaReader size
    else failwithf "wrong bits"

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
        | -1 -> failwithf "pack source not found, git repo is corrupted"
        | _ -> parsePackInfo
               <| getPackPath path (Option.toObj containingPack) ".pack"
               <| offset
               <| flag
