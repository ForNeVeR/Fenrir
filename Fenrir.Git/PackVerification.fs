// SPDX-FileCopyrightText: 2021-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module internal Fenrir.Git.PackVerification
// https://git-scm.com/docs/pack-format

open System.IO
open System.Security.Cryptography
open System.Text
open Fenrir.Git.Metadata
open Fenrir.Git.DeltaCommands
open Fenrir.Git.Tools
open Fenrir.Git.Packing
open Microsoft.FSharp.Core

let verifyPackHeader (reader: BinaryReader) : int =
    if reader.ReadBytes(4) <> "PACK"B then
        failwith "corrupted pack header signature"

    let version = reader.ReadBigEndianInt()

    if version <> 2 && version <> 3 then
        failwith "corrupted pack header version"

    // objects count
    reader.ReadBigEndianInt()


type BaseRef =
    | Hash of Sha1Hash
    | Offset of int64
    | NotRef

type PackObjectInfo = {
    Type: PackedObjectType
    PackedSize: int64
    Size: int64
    Offset: int64
    Ref: BaseRef
}

type VerifyPackObjectInfo = {
    Type: GitObjectType
    Size: int64
    PackedSize: int64
    Offset: int64
    Depth: int
    Hash: Sha1Hash
    BaseHash: Sha1Hash option
}

let getTypeName (objectType: GitObjectType) =
    match objectType with
    | GitObjectType.GitBlob -> "blob"
    | GitObjectType.GitCommit -> "commit"
    | GitObjectType.GitTree -> "tree"
    | _ -> failwith "Can't find object type name"

let unpack (reader: BinaryReader) (size: int) =
    use packedStream =
        new MemoryStream(size + 20 |> reader.ReadBytes)

    let unpackedStream = new MemoryStream()

    let packedSize =
        Zlib.unpackObjectAndReturnPackedLength packedStream unpackedStream

    (packedSize, unpackedStream)

let resolveDeltaChain (reader: BinaryReader) (root: PackObjectInfo) (refDeltas: PackObjectInfo list) (ofsDeltas: PackObjectInfo list): seq<VerifyPackObjectInfo> =
    let calcHash (objectType: GitObjectType) (dataStream: MemoryStream) =
        use sha1 = SHA1.Create()
        let objectTypeName = getTypeName objectType

        let bytes =
            [| Encoding.ASCII.GetBytes($"{objectTypeName} {string dataStream.Length}\u0000")
               dataStream.ToArray() |]
            |> Array.concat

        dataStream.Seek(0L, SeekOrigin.Begin) |> ignore

        // TODO[#115]: Use utilities from the hardened Sha1 module and not system SHA-1 implementation.
        bytes |> sha1.ComputeHash |> Sha1Hash.OfBytes

    let rec resolveObject (object: PackObjectInfo) (baseObject: VerifyPackObjectInfo option) (baseObjectData: MemoryStream) (depth: int) =
        reader.BaseStream.Seek(object.Offset, SeekOrigin.Begin) |> ignore
        getObjectMeta reader |> ignore
        if object.Type = PackedObjectType.OfsDelta then getNegOffset reader |> ignore
        if object.Type = PackedObjectType.RefDelta then reader.ReadHash() |> ignore

        let objectType =
            if Option.isSome baseObject
            then baseObject.Value.Type
            else match object.Type with
                 | PackedObjectType.Commit
                 | PackedObjectType.Tag -> GitObjectType.GitCommit
                 | PackedObjectType.Tree -> GitObjectType.GitTree
                 | PackedObjectType.Blob -> GitObjectType.GitBlob
                 | _ -> failwith "Impossible!"
        let _, data = unpack reader (int object.Size)

        let dataToHash =
            if depth = 0 then
                data
            else
                use nonDeltaReader =
                    new BinaryReader(baseObjectData, Encoding.UTF8, true)

                data.Seek(0L, SeekOrigin.Begin) |> ignore
                processDelta data nonDeltaReader (int object.Size)

        let obj: VerifyPackObjectInfo = {
            Type = objectType
            Size = object.Size
            PackedSize = object.PackedSize
            Offset = object.Offset
            Depth = depth
            Hash = calcHash objectType dataToHash
            BaseHash = if Option.isSome baseObject then Some(baseObject.Value.Hash) else None
        }

        let descendants = List.concat [List.filter (fun (o: PackObjectInfo) -> o.Ref = Hash obj.Hash) refDeltas
                                       List.filter (fun (o: PackObjectInfo) -> o.Ref = Offset obj.Offset) ofsDeltas]

        seq {
            yield obj
            for d in descendants do
                yield! resolveObject d (Some obj) dataToHash (depth + 1)
        }


    seq {
        yield! resolveObject root None (new MemoryStream()) 0
    }

let resolveDeltas (reader: BinaryReader) (nonDeltas: PackObjectInfo list) (refDeltas: PackObjectInfo list) (ofsDeltas: PackObjectInfo list) =
    seq {
        for nonDelta in nonDeltas do
            yield! (resolveDeltaChain reader nonDelta refDeltas ofsDeltas)
    }

let parseObjects (reader: BinaryReader) (objectsCount: int) : VerifyPackObjectInfo list =
    let mutable nonDeltas = list.Empty
    let mutable refDeltas = list.Empty
    let mutable ofsDeltas = list.Empty

    for _ in [1..objectsCount] do
        let objectStart = reader.BaseStream.Position
        let size, packedObjectType = getObjectMeta reader

        let parsedObject =
            match packedObjectType with
            | PackedObjectType.Commit
            | PackedObjectType.Tree
            | PackedObjectType.Blob
            | PackedObjectType.Tag ->
                let headerSize =reader.BaseStream.Position - objectStart
                let packedSize, unpackedStream = unpack reader size

                nonDeltas <-  {
                    Type = packedObjectType
                    PackedSize = packedSize + headerSize
                    Size = unpackedStream.Length
                    Offset = objectStart
                    Ref = NotRef
                }           ::nonDeltas
                nonDeltas.Head

            | PackedObjectType.OfsDelta
            | PackedObjectType.RefDelta ->
                let baseRef =
                    match packedObjectType with
                    | PackedObjectType.RefDelta ->
                        Hash (reader.ReadHash())
                    | PackedObjectType.OfsDelta ->
                        Offset (objectStart - int64 (getNegOffset reader))
                    | _ -> failwith "Impossible!"

                let headerSize = reader.BaseStream.Position - objectStart

                let packedSize, unpackedStream = unpack reader size

                let o = {
                    Type = packedObjectType
                    PackedSize = packedSize + headerSize
                    Size = unpackedStream.Length
                    Offset = objectStart
                    Ref = baseRef
                }
                match packedObjectType with
                    | PackedObjectType.RefDelta ->
                        refDeltas <- o :: refDeltas
                        o
                    | PackedObjectType.OfsDelta ->
                        ofsDeltas <- o :: ofsDeltas
                        o
                    | _ -> failwith "Impossible!"

            | o -> failwithf $"Cannot parse object type from a pack file: %A{o}"

        if parsedObject.Size <> int64 size then
            failwith $"Size in packfile {parsedObject.Size} and real size {size} for offset {parsedObject.Offset} are different"

        reader.BaseStream.Seek(objectStart + parsedObject.PackedSize, SeekOrigin.Begin)
        |> ignore

    resolveDeltas reader nonDeltas refDeltas ofsDeltas |> Seq.sortBy (fun (i: VerifyPackObjectInfo) -> i.Offset) |> List.ofSeq

let calcDepthDistribution (objects: VerifyPackObjectInfo list) : Map<int, int> =
    objects
    |> Seq.groupBy (fun (o: VerifyPackObjectInfo) -> o.Depth)
    |> Seq.map (fun (k, v) -> k, Seq.length v)
    |> Map.ofSeq

let verifyPackHash (reader: BinaryReader) (lastObject: VerifyPackObjectInfo) =
    let objectsEnd =
        lastObject.Offset + lastObject.PackedSize

    reader.BaseStream.Seek(0L, SeekOrigin.Begin)
    |> ignore

    // TODO[#115]: Use the hardened Sha1 module and not the system SHA-1 implementation.
    use sha1 = SHA1.Create()

    let hash =
        reader.ReadBytes(int objectsEnd)
        |> sha1.ComputeHash
        |> Sha1Hash.OfBytes
    let hashFromPack = reader.ReadHash()

    if hash <> hashFromPack then
        failwith "Packfile hash corrupted"

    if reader.BaseStream.Position <> reader.BaseStream.Length then
        failwith "Packfile has data after pack content"

let printObjects (objects: VerifyPackObjectInfo list) : seq<string> =
    objects
    |> Seq.map
        (fun object ->
            $"{object.Hash} %-6s{getTypeName object.Type} {object.Size} {object.PackedSize} {object.Offset}"
            + if Option.isSome object.BaseHash then
                  $" {object.Depth} {object.BaseHash.Value}"
              else
                  "")

let printHistogram (depths: Map<int, int>) : seq<string> =
    let pluralise (i: int) = if i = 1 then "object" else "objects"

    Map.toSeq depths
    |> Seq.sortBy fst
    |> Seq.map
        (fun (depth, count) ->
            if depth = 0 then
                $"non delta: {count} {pluralise count}"
            else
                $"chain length = {depth}: {count} {pluralise count}")

let Verify (reader: BinaryReader) (verbose: bool): seq<string> =
    let objectsCount = verifyPackHeader reader
    let objects = parseObjects reader objectsCount
    let depths = calcDepthDistribution objects

    verifyPackHash reader (List.last objects)

    let objSeq =
        if verbose then
            printObjects objects
        else
            Seq.empty

    printHistogram depths |> Seq.append objSeq
