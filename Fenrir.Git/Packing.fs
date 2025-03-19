// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// <summary>Functions to operate on <a href="https://git-scm.com/docs/pack-format">pack files</a>.</summary>
module Fenrir.Git.Packing

open System
open System.Collections
open System.IO

open System.Threading.Tasks
open Fenrir.Git.DeltaCommands
open Fenrir.Git.Metadata
open Fenrir.Git.Tools
open Fenrir.Git.Zlib

let private getPackPath (gitPath: String) (packFile: String) (extension: String) : String =
    Path.Combine(gitPath, "objects", "pack", packFile + extension)

/// <summary>Read more in <a href="https://git-scm.com/docs/pack-format">the pack format documentation.</a></summary>
type PackedObjectType =
    | Commit = 1
    | Tree = 2
    | Blob = 3
    | Tag = 4
    | OfsDelta = 6
    | RefDelta = 7

let private parseIndexOffset (path: String) (hash: String) : int =
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

let private getObjectKind(bits: BitArray): PackedObjectType =
    let array = Array.zeroCreate 1
    bits.CopyTo(array, 0)
    enum array.[0]

/// An unpacked form of an object in a pack file.
type PackedObject =
    {
        ObjectType: GitObjectType
        Stream: Stream
    }
    interface IDisposable with
        member this.Dispose() = this.Stream.Dispose()

let internal getObjectMeta (reader: BinaryReader): int * PackedObjectType =
    let sizeBytes = readWhileLast isMsbSet (uint64 reader.BaseStream.Length) reader
    let bits = sizeBytes.[0] |> byteToBits
    let mutable size = sliceBitArray bits 0 3 |> bitsToInt

    size <- estimateSize size 4 sizeBytes.[1..]
    let objectMask = sliceBitArray bits 4 6
    (size, getObjectKind objectMask)

let internal getNegOffset (reader: BinaryReader): int =
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

let rec private parsePackInfo (index: PackIndex) (location: PackedObjectLocation): Task<PackedObject> = task {
    use packReader = new BinaryReader(File.Open(location.PackFile.Value,
                                                FileMode.Open,
                                                FileAccess.Read,
                                                FileShare.Read))
    packReader.BaseStream.Position <- int64 location.Offset

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
        return {
            ObjectType = objectType
            Stream = stream
         }

    | PackedObjectType.OfsDelta | PackedObjectType.RefDelta->
        let! deltifiedEntity = task {
            match packedObjectType with
            | PackedObjectType.OfsDelta ->
                let negOffset = getNegOffset packReader
                return! parsePackInfo index { location with Offset = location.Offset - Checked.uint32 negOffset }
            | PackedObjectType.RefDelta ->
                let hash = packReader.ReadHash()
                let! object = ReadPackedObject(index, hash)
                return nonNull object
            | _ -> return failwith $"Can't get deltified entry for non delta object type {packedObjectType}."
        }


        use nonDeltaReader = new BinaryReader(deltifiedEntity.Stream)
        use stream = new MemoryStream(size + 20 |> packReader.ReadBytes) |> getDecodedStream
        return {
            deltifiedEntity with
                Stream = processDelta stream nonDeltaReader size
        }
    | o -> return failwithf $"Cannot parse object type from a pack file: {o}."
}

and ReadPackedObject(index: PackIndex, hash: string): Task<PackedObject | null> = task {
    let! location = index.FindPackOfObject hash
    let result: Task<PackedObject | null> =
        if location.HasValue then
            parsePackInfo index location.Value
        else
            Task.FromResult<PackedObject | null>(null)
    return! result
}
