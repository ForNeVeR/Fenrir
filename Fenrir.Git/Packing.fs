// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Git.Packing

open System
open System.Collections
open System.IO

open Fenrir.Git.DeltaCommands
open Fenrir.Git.Metadata
open Fenrir.Git.Tools
open Fenrir.Git.Zlib

let getPackPath (gitPath: String) (packFile: String) (extension: String) : String =
    Path.Combine(gitPath, "objects", "pack", packFile + extension)

// https://git-scm.com/docs/pack-format
type PackedObjectType =
    | Commit = 1
    | Tree = 2
    | Blob = 3
    | Tag = 4
    | OfsDelta = 6
    | RefDelta = 7

let parseIndexOffset (path: String) (hash: String) : int =
    use idxReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    //skip header and fanout table
    idxReader.BaseStream.Position <- 1028L
    //last item in fanout table
    let size = idxReader.ReadBigEndianInt()
    //hashes extraction
    let hashes = Array.init size (fun _ -> idxReader.ReadHash())
    //position binary search of the hash
    let pos = Array.BinarySearch(hashes, hash)
    //skipping crc table and getting offset location
    if pos >= 0 then
        idxReader.BaseStream.Position <-
            idxReader.BaseStream.Position + int64 size * 4L
            + (int64 pos) * 4L

        idxReader.ReadBigEndianInt()
    else
        -1

let getObjectKind(bits: BitArray): PackedObjectType =
    let array = Array.zeroCreate 1
    bits.CopyTo(array, 0)
    enum array.[0]

type PackedObjectInfo =
    {
        ObjectType: GitObjectType
        Stream: MemoryStream
    }
    interface IDisposable with
        member this.Dispose() = this.Stream.Dispose()

let getObjectMeta (reader: BinaryReader): int * PackedObjectType =
    let sizeBytes = readWhileLast isMsbSet (uint64 reader.BaseStream.Length) reader
    let bits = sizeBytes.[0] |> byteToBits
    let mutable size = sliceBitArray bits 0 3 |> bitsToInt

    size <- estimateSize size 4 sizeBytes.[1..]
    let objectMask = sliceBitArray bits 4 6
    (size, getObjectKind objectMask)

let getNegOffset (reader: BinaryReader): int =
    let mutable additional = 0
    let mutable offStep = 0
    (readWhileLast isMsbSet (uint64 reader.BaseStream.Length) reader
        |> Array.fold (fun acc elem ->
            additional <- additional + (1 <<< offStep)
            offStep <- offStep + 7
            (acc <<< 7) ||| int (elem % 128uy)
            ) 0
        )
        + additional - 1

let rec parsePackInfo (path: String) (offset: int) (getPackedObject: String -> String -> PackedObjectInfo) : PackedObjectInfo =
    use packReader = new BinaryReader(File.Open(path,
                                                FileMode.Open,
                                                FileAccess.Read,
                                                FileShare.Read))
    packReader.BaseStream.Position <- int64 offset

    let size, packedObjectType = getObjectMeta packReader
    match packedObjectType with
    | PackedObjectType.Commit | PackedObjectType.Tree | PackedObjectType.Blob | PackedObjectType.Tag ->
        let stream = new MemoryStream(size + 20 |> packReader.ReadBytes) |> getDecodedStream
        let objectType =
            match packedObjectType with
            | PackedObjectType.Commit | PackedObjectType.Tag -> GitObjectType.GitCommit
            | PackedObjectType.Tree -> GitObjectType.GitTree
            | PackedObjectType.Blob -> GitObjectType.GitBlob
            | _ -> failwith "Impossible!"
        { ObjectType = objectType
          Stream = stream }

    | PackedObjectType.OfsDelta | PackedObjectType.RefDelta->
        use deltifiedEntity =
            match packedObjectType with
            | PackedObjectType.OfsDelta ->
                let negOffset = getNegOffset packReader
                parsePackInfo path (offset - negOffset) getPackedObject
            | PackedObjectType.RefDelta ->
                let hash = packReader.ReadHash()
                getPackedObject path hash
            | _ -> failwith $"Can't get deltified entry for non delta object type {packedObjectType}"

        use nonDeltaReader = new BinaryReader(deltifiedEntity.Stream)
        use stream = new MemoryStream(size + 20 |> packReader.ReadBytes) |> getDecodedStream
        { deltifiedEntity with
            Stream = processDelta stream nonDeltaReader size }
    | o -> failwithf "Cannot parse object type from a pack file: %A" o

let rec getPackedObject (path: String) (hash: String): PackedObjectInfo =
    let packs = Directory.GetFiles(Path.Combine(path, "objects", "pack"), "*.idx")
                |> Seq.map (fun x -> Path.GetFileName x |> nonNull)
                |> Seq.map ((fun count (str : string) -> str.[0..str.Length - count - 1]) 4)
                |> Seq.toArray
    let mutable offset = -1

    let containingPack = packs |> Array.tryFind (fun item ->
        offset <- parseIndexOffset
                  <| getPackPath path item ".idx"
                  <| hash
        offset <> -1)

    match offset with
        | -1 -> failwith $"pack source for {hash} not found, git repo is corrupted"
        | _ -> parsePackInfo
               <| getPackPath path (Option.get containingPack) ".pack"
               <| offset
               <| getPackedObject
