namespace Fenrir.Git

open System
open System.Buffers.Binary
open System.Collections.Generic
open System.IO
open System.IO.MemoryMappedFiles
open System.Threading.Tasks
open FSharp.Control
open Fenrir.Git.Tools
open JetBrains.Lifetimes
open Microsoft.FSharp.NativeInterop
open TruePath

[<Struct>]
type PackedObjectLocation = {
    PackFile: LocalPath
    Offset: uint32
}

type private PackFile(
    lifetime: Lifetime,
    path: LocalPath,
    index: MemoryMappedFile,
    fanoutTable: uint32[]
) =
    let objectNameTableOffset =
            4u // magic
            + 4u // version
            + 256u * uint32 sizeof<uint32> // fanout table

    let objectCount = fanoutTable[255]

    let binarySearch (hash: Sha1Hash) (offsetFrom: uint32) offsetTo =
        let initialReadOffset = objectNameTableOffset + offsetFrom * uint32 sizeof<Sha1Hash>
        let enumeratedEntryCount = offsetTo - offsetFrom
        assert (enumeratedEntryCount >= 0u)

        if enumeratedEntryCount = 0u then ValueNone
        else

        let data = Array.zeroCreate(Checked.int32 enumeratedEntryCount)
        let readEntries = lifetime.Execute(fun() ->
            use accessor = index.CreateViewAccessor(
                int64 initialReadOffset,
                int64 enumeratedEntryCount * int64 sizeof<Sha1Hash>,
                MemoryMappedFileAccess.Read
            )
            accessor.ReadArray(0, data, 0, Checked.int32 enumeratedEntryCount)
        )
        assert (uint32 readEntries = enumeratedEntryCount)

        let index = Array.BinarySearch(data, hash)
        if index = -1 then ValueNone else ValueSome(uint32 index + offsetFrom)

    let readOffsetForIndex offsetIndex =
        let readOffset =
            objectNameTableOffset
            + objectCount * uint32 Sha1Hash.SizeInBytes
            + objectCount * uint32 sizeof<uint32> // CRC32 table
            + offsetIndex * uint32 sizeof<uint32>
        lifetime.Execute(fun() ->
            use accessor = index.CreateViewStream(0, 0, MemoryMappedFileAccess.Read)
            accessor.Position <- int64 readOffset
            let offset = accessor.ReadBigEndianUInt32()
            // TODO: work with 8-byte offsets
            //       https://git-scm.com/docs/pack-format — "A table of 8-byte offset entries […]"
            offset
        )

    member _.Path = path

    member _.TryFindHashOffset(hash: Sha1Hash): ValueOption<uint32> =
        let firstByte = hash.Byte0
        let objectsWithFirstByteLessOrEqualCurrent = fanoutTable[int firstByte]
        let objectsWithFirstByteLessThanCurrent = if firstByte = 0uy then 0u else fanoutTable[int firstByte - 1]

        let hashIndex = binarySearch hash objectsWithFirstByteLessThanCurrent objectsWithFirstByteLessOrEqualCurrent
        hashIndex |> ValueOption.map readOffsetForIndex

#nowarn 9 // stackalloc usage below makes an assembly "unverifiable"

/// <summary>
///     <para>
///         Type to store, cache and access information about
///         <a href="https://git-scm.com/docs/pack-format">Git pack files</a>.
///     </para>
///     <para>This type is thread-safe.</para>
/// </summary>
/// <remarks>
/// This type is optimized for two scenarios:
/// <list type="number">
///     <item>
///         <b>One-time usage (read-and-forget)</b>, for scenarios involving reading one object only.
///         To target these scenarios,
///         we make sure to not read all the indices ahead of time if they might not be used for an operation.
///     </item>
///     <item>
///         <b>Multi-read usage (cache)</b>, for a scenario involving prolonged use
///         (e.g., indexing of the whole repository).
///         To target such scenarios, we allow reading and storing the fanout tables of all the pack index files at
///         once.
///     </item>
/// </list>
/// </remarks>
type PackIndex

    /// <summary>Creates a pack index cached for the designated repository.</summary>
    /// <param name="lifetime">
    /// Lifetime of the cache.
    /// No file access will be performed after the lifetime's termination.
    /// </param>
    /// <param name="gitDir">Path to the <c>.git</c> directory of a repository.</param>
    (lifetime: Lifetime, gitDir: LocalPath) =

    let ReadFanoutTable(index: UnmanagedMemoryStream) = task {
        let items = Array.zeroCreate(256 * sizeof<uint32>)
        do! index.ReadExactlyAsync(items, 0, items.Length, lifetime.ToCancellationToken())
        let fanoutTable = Array.init 256 (fun i ->
            BinaryPrimitives.ReadUInt32BigEndian(ReadOnlySpan(items, i * sizeof<uint32>, sizeof<uint32>))
        )
        return fanoutTable
    }

    let LoadPackFile(pack: LocalPath) =  lifetime.ExecuteAsync(fun() -> task {
        let index = LocalPath(nonNull <| Path.ChangeExtension(pack.Value, "idx"))
        let indexMapping = MemoryMappedFile.CreateFromFile(
            index.Value,
            FileMode.Open,
            mapName = null,
            capacity = 0,
            access = MemoryMappedFileAccess.Read
        )
        lifetime.AddDispose indexMapping |> ignore
        use stream = indexMapping.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read)

        let magic = Span<byte>(NativePtr.toVoidPtr <| NativePtr.stackalloc<byte> 4, 4)
        stream.ReadExactly magic
        if not <| magic.SequenceEqual [| 0xFFuy; 0x74uy; 0x4Fuy; 0x63uy |] then
            failwithf $"Unknown magic value in the index file \"{index}\": {Convert.ToHexString magic}."

        let version = stream.ReadBigEndianUInt32()
        if version <> 2u then
            failwithf $"Unknown version value in the index file \"{index}\": {version}."

        let! fanoutTable = ReadFanoutTable stream
        return PackFile(lifetime, pack, indexMapping, fanoutTable)
    })

    let packs = lazy (
        let packFileDir = gitDir / "objects" / "pack"
        lifetime.Execute(fun() ->
            if Directory.Exists packFileDir.Value then
                Directory.EnumerateFileSystemEntries(packFileDir.Value, "*.pack")
                |> Seq.map LocalPath
                |> Seq.map(fun packPath -> KeyValuePair(packPath, lazy (LoadPackFile packPath)))
                |> Dictionary
            else
                Dictionary()
        )
    )

    /// <summary>Searches an object with <paramref name="hash"/> among the pack files included in the index.</summary>
    /// <param name="hash">Hash of the searched object.</param>
    /// <returns>Path to a pack file containing the object.</returns>
    member _.FindPackOfObject(hash: Sha1Hash): Task<Nullable<PackedObjectLocation>> =
        Async.StartAsTask(async {
            let! searchResult =
                packs.Value.Values
                |> AsyncSeq.ofSeq
                |> AsyncSeq.tryPickAsync (fun packFileTask -> async {
                    let! packFile = Async.AwaitTask packFileTask.Value
                    let offset = packFile.TryFindHashOffset hash
                    return
                        match offset with
                        | ValueNone -> None
                        | ValueSome offset -> Some(struct (packFile.Path, offset))
                })
            return
                match searchResult with
                | Some(struct (path, offset)) -> Nullable({ PackFile = path; Offset = offset })
                | None -> Nullable()
        }, cancellationToken = lifetime.ToCancellationToken())
