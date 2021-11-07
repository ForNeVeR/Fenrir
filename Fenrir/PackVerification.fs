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



type VerifyPackObjectInfo(objType: GitObjectType, packedSize: int64, size: int64, offset: int64, depth: int) =
    let mutable children: VerifyPackObjectInfo list = []

    member this.Type = objType
    member this.Size = int size
    member this.PackedSize = packedSize
    member this.Offset = offset
    member this.Depth = depth

    member val Hash = "" with get, set
    member val BaseHash: string option = None with get, set

    member this.Children = children
    member this.AddChild child = children <- child :: children

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

let rec calculateHashesRecursive (reader: BinaryReader) (object: VerifyPackObjectInfo) (baseDataStream: MemoryStream) (baseHash: string option) : unit =
    let calcHash (objectType: GitObjectType) (dataStream: MemoryStream) =
        let sha1 = HashAlgorithm.Create("SHA1")
        let objectTypeName = getTypeName objectType

        let bytes =
            [| Encoding.ASCII.GetBytes($"{objectTypeName} {dataStream.Length}\u0000")
               dataStream.ToArray() |]
            |> Array.concat

        dataStream.Seek(0L, SeekOrigin.Begin) |> ignore

        (bytes |> sha1.ComputeHash |> Convert.ToHexString)
            .ToLower()

    reader.BaseStream.Seek(object.Offset, SeekOrigin.Begin)
    |> ignore

    let _, packedObjectType = getObjectMeta reader

    if packedObjectType = PackedObjectType.OfsDelta then
        getNegOffset reader |> ignore

    if packedObjectType = PackedObjectType.RefDelta then
        reader.ReadHash() |> ignore

    let _, data = unpack reader object.Size

    let dataToHash =
        if object.Depth = 0 then
            data
        else
            use nonDeltaReader =
                new BinaryReader(baseDataStream, Encoding.UTF8, true)

            data.Seek(0L, SeekOrigin.Begin) |> ignore
            processDelta data nonDeltaReader object.Size

    if object.Hash = "" then
        object.Hash <- calcHash object.Type dataToHash

    object.BaseHash <- baseHash

    for child in object.Children do
        calculateHashesRecursive reader child dataToHash (Some object.Hash)


let calculateHashes (reader: BinaryReader) (objects: VerifyPackObjectInfo list) : unit =
    for object in List.filter (fun (o: VerifyPackObjectInfo) -> o.Depth = 0) objects do
        calculateHashesRecursive reader object (new MemoryStream()) None

let parseObjects (reader: BinaryReader) (objectsCount: int) : VerifyPackObjectInfo list =
    let mutable parsedObjects = list.Empty

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

                VerifyPackObjectInfo(objectType, packedSize + sizeBytesLength, unpackedStream.Length, objectStart, 0)

            | PackedObjectType.OfsDelta
            | PackedObjectType.RefDelta ->
                let posBeforeBaseLocationRead = reader.BaseStream.Position

                let baseObj =
                    match packedObjectType with
                    | PackedObjectType.RefDelta ->
                        calculateHashes reader parsedObjects

                        reader.BaseStream.Seek(posBeforeBaseLocationRead, SeekOrigin.Begin)
                        |> ignore

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

                let obj =
                    VerifyPackObjectInfo(
                        baseObj.Type,
                        packedSize
                        + sizeBytesLength
                        + baseObjectLocationSize,
                        unpackedStream.Length,
                        objectStart,
                        baseObj.Depth + 1
                    )

                baseObj.AddChild obj
                obj
            | o -> failwithf $"Cannot parse object type from a pack file: %A{o}"

        if parsedObject.Size <> size then
            failwith $"Size in packfile {parsedObject.Size} and real size {size} for {parsedObject.Hash} are different"

        reader.BaseStream.Seek(objectStart + parsedObject.PackedSize, SeekOrigin.Begin)
        |> ignore

        parsedObjects <- parsedObject :: parsedObjects

    calculateHashes reader parsedObjects
    parsedObjects

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

    let sha1 = HashAlgorithm.Create("SHA1")

    let hash =
        reader.ReadBytes(int objectsEnd)
        |> sha1.ComputeHash
        |> Convert.ToHexString
    let hashFromPack = reader.ReadHash()

    if not (hash.Equals(hashFromPack, StringComparison.InvariantCultureIgnoreCase)) then
        failwith "Packfile hash corrupted"

    if reader.BaseStream.Position <> reader.BaseStream.Length then
        failwith "Packfile has data after pack content"

let printObjects (objects: VerifyPackObjectInfo list) : seq<string> =
    seq {
        for object in objects do
            $"{object.Hash} %-6s{getTypeName object.Type} {object.Size} {object.PackedSize} {object.Offset}"
            + if Option.isSome object.BaseHash then
                  $" {object.Depth} {object.BaseHash.Value}"
              else
                  ""
    }

let printHistogram (depths: Map<int, int>) : seq<string> =
    let pluralise (i: int) = if i = 1 then "object" else "objects"

    seq {
        for depth, count in Map.toSeq depths |> Seq.sortBy fst do
            if depth = 0 then
                $"non delta: {count} {pluralise count}"
            else
                $"chain length = {depth}: {count} {pluralise count}"
    }


let verifyPack (reader: BinaryReader) (verbose: bool) : seq<string> =
    let objectsCount = verifyPackHeader reader
    let objects = parseObjects reader objectsCount
    let depths = calcDepthDistribution objects

    verifyPackHash reader objects.Head

    let objSeq =
        if verbose then
            printObjects (List.rev objects)
        else
            Seq.empty

    printHistogram depths |> Seq.append objSeq
