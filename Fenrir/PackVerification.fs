module Fenrir.PackVerification
// https://git-scm.com/docs/pack-format

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Fenrir.Metadata
open Fenrir.DeltaCommands
open Fenrir.Tools
open Fenrir.Packing
open Microsoft.FSharp.Core

let verifyPackHeader (reader: BinaryReader) : int =
    if reader.ReadBytes(4) <> "PACK"B then
        failwith "corrupted pack header signature"

    let version = reader.GetBigEndian()

    if version <> 2 && version <> 3 then
        failwith "corrupted pack header version"

    // objects count
    reader.GetBigEndian()

type VerifyPackObjectInfo =
    { Hash: string
      Type: GitObjectType
      Size: int64
      PackedSize: int64
      Offset: int64
      Depth: int
      Data: byte array
      BaseHash: string option }

let getTypeName (objectType: GitObjectType) =
    match objectType with
    | GitObjectType.GitBlob -> "blob"
    | GitObjectType.GitCommit -> "commit"
    | GitObjectType.GitTree -> "tree"
    | _ -> failwith "Can't find object type name"

let parseObjects (reader: BinaryReader) (objectsCount: int) : VerifyPackObjectInfo list =
    let mutable parsedObjects = list.Empty

    let sha1 = HashAlgorithm.Create("SHA1")

    let unpack (reader: BinaryReader) (size: int) =
        use packedStream =
            new MemoryStream(size + 20 |> reader.ReadBytes)

        let unpackedStream = new MemoryStream()

        let packedSize =
            Zlib.unpackObjectAndReturnPackedLength packedStream unpackedStream

        (packedSize, unpackedStream)

    let calcHash (objectType: GitObjectType) (dataStream: MemoryStream) =
        let objectTypeName = getTypeName objectType

        let bytes =
            [| Encoding.ASCII.GetBytes($"{objectTypeName} {dataStream.Length}\u0000")
               dataStream.ToArray() |]
            |> Array.concat

        dataStream.Seek(0L, SeekOrigin.Begin) |> ignore

        (bytes |> sha1.ComputeHash |> Convert.ToHexString)
            .ToLower()

    while parsedObjects.Length <> objectsCount do
        let objectStart = reader.BaseStream.Position
        let size, packedObjectType = getObjectMeta reader
        let sizeBytesLength = reader.BaseStream.Position - objectStart

        let parsedObject =
            match packedObjectType with
            | PackedObjectType.Commit
            | PackedObjectType.Tree
            | PackedObjectType.Blob
            | PackedObjectType.Tag ->
                let packedSize, unpackedStream = unpack reader size

                let objectType =
                    match packedObjectType with
                    | PackedObjectType.Commit
                    | PackedObjectType.Tag -> GitObjectType.GitCommit
                    | PackedObjectType.Tree -> GitObjectType.GitTree
                    | PackedObjectType.Blob -> GitObjectType.GitBlob
                    | _ -> failwith "Impossible!"

                let hash = calcHash objectType unpackedStream

                { Hash = hash
                  Type = objectType
                  PackedSize = packedSize + sizeBytesLength
                  Size = unpackedStream.Length
                  Offset = objectStart
                  Depth = 0
                  Data = unpackedStream.ToArray()
                  BaseHash = None }

            | PackedObjectType.OfsDelta
            | PackedObjectType.RefDelta ->
                let posBeforeBaseLocationRead = reader.BaseStream.Position

                let baseObj =
                    match packedObjectType with
                    | PackedObjectType.RefDelta ->
                        let hash = reader.ReadHash()
                        List.find (fun (i: VerifyPackObjectInfo) -> i.Hash = hash) parsedObjects
                    | PackedObjectType.OfsDelta ->

                        let baseOffset =
                            objectStart - int64 (getNegOffset reader)

                        List.find (fun (i: VerifyPackObjectInfo) -> i.Offset = baseOffset) parsedObjects
                    | _ -> failwith "Impossible!"

                let baseObjectLocationSize =
                    reader.BaseStream.Position
                    - posBeforeBaseLocationRead

                let packedSize, unpackedStream = unpack reader size

                use nonDeltaReader =
                    new BinaryReader(new MemoryStream(baseObj.Data))

                unpackedStream.Seek(0L, SeekOrigin.Begin)
                |> ignore

                let undeltifiedStream =
                    processDelta unpackedStream nonDeltaReader size

                let hash = calcHash baseObj.Type undeltifiedStream

                { Hash = hash
                  Type = baseObj.Type
                  PackedSize =
                      packedSize
                      + sizeBytesLength
                      + baseObjectLocationSize
                  Size = unpackedStream.Length
                  Offset = objectStart
                  Depth = baseObj.Depth + 1
                  Data = undeltifiedStream.ToArray()
                  BaseHash = Some baseObj.Hash }
            | o -> failwithf $"Cannot parse object type from a pack file: %A{o}"

        if parsedObject.Size <> int64 size then
            failwith $"Size in packfile {parsedObject.Size} and real size {size} for {parsedObject.Hash} are different"

        reader.BaseStream.Seek(objectStart + parsedObject.PackedSize, SeekOrigin.Begin)
        |> ignore

        parsedObjects <- parsedObject :: parsedObjects

    List.rev parsedObjects

let calcDepthDistribution (objects: VerifyPackObjectInfo list) : Map<int, int> =
    objects
    |> Seq.groupBy (fun (o: VerifyPackObjectInfo) -> o.Depth)
    |> Seq.map (fun (k, v) -> k, Seq.length v)
    |> Map.ofSeq

let printObjects (objects: VerifyPackObjectInfo list) : seq<string> =
    seq {
        for object in objects do
            $"{object.Hash} %-6s{getTypeName object.Type} {object.Size} {object.PackedSize} {object.Offset}"
            + if Option.isSome object.BaseHash then
                  $" {object.Depth} {object.BaseHash.Value}"
              else
                  ""
    }

let printHistogram (depths:Map<int, int>) : seq<string> =
    let pluralise (i: int) = if i = 1 then "object" else "objects"

    seq {
        for depth, count in Map.toSeq depths |> Seq.sortBy fst do
            if depth = 0 then $"non delta: {count} {pluralise count}"
            else $"chain length = {depth}: {count} {pluralise count}"
    }


let verifyPack (reader: BinaryReader) (verbose: bool) : seq<string> =
    let objectsCount = verifyPackHeader reader
    let objects = parseObjects reader objectsCount
    let depths = calcDepthDistribution objects

    let objSeq =
        if verbose then
            printObjects objects
        else
            Seq.empty

    printHistogram depths |> Seq.append objSeq
