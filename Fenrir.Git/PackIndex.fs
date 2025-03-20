namespace Fenrir.Git

open System
open System.Collections.Generic
open System.IO
open System.Threading.Tasks
open FSharp.Control
open JetBrains.Lifetimes
open TruePath

[<Struct>]
type PackedObjectLocation = {
    PackFile: LocalPath
    Offset: uint32
}

type private PackFile(path: LocalPath) =
    member _.Path = path
    member _.TryFindHashOffset(_hash: string): ValueOption<uint32> =
        failwith "TODO TryFindHashOffset"

/// <summary>
///     <para>Type to store, cache and access information about Git pack files.</para>
///     <para>This type is thread-safe.</para>
/// </summary>
/// <param name="lifetime">
/// Lifetime of the cache.
/// No file access will be performed after the lifetime's termination.
/// </param>
/// <param name="gitDir">Path to the <c>.git</c> directory of a repository.</param>
/// <remarks>
/// This type is optimized for two scenarios:
/// <list type="number">
///     <item>
///         <b>One-time usage (read-and-forget)</b>, for scenarios involving reading one object only. To target these
///         scenarios, we make sure to not read all the indices ahead of time if they might not be used for an
///         operation.
///     </item>
///     <item>
///         <b>Multi-read usage (cache)</b>, for a scenario involving prolonged use (e.g., indexing of the whole
///         repository). To target such scenarios, we allow reading and store the fanout tables of all the pack index
///         files at once.
///     </item>
/// </list>
/// </remarks>
type PackIndex(lifetime: Lifetime, gitDir: LocalPath) =

    let loadPackFile path = task {
        return PackFile path
    }

    let packs = lazy (
        let packFileDir = gitDir / "objects" / "pack"
        lifetime.Execute(fun() ->
            if Directory.Exists packFileDir.Value then
                Directory.EnumerateFileSystemEntries(packFileDir.Value, "*.pack")
                |> Seq.map LocalPath
                |> Seq.map(fun packPath -> KeyValuePair(packPath, lazy (loadPackFile packPath)))
                |> Dictionary
            else
                Dictionary()
        )
    )

    /// <summary>Searches an object with <paramref name="hash"/> among the pack files included in the index.</summary>
    /// <param name="hash">Hash of the searched object.</param>
    /// <returns>Path to a pack file containing the object.</returns>
    member _.FindPackOfObject(hash: string): Task<Nullable<PackedObjectLocation>> =
        task {
            let! searchResult =
                packs.Value.Values
                |> AsyncSeq.ofSeq
                |> AsyncSeq.tryPickAsync (fun packFileTask -> async {
                    let! packFile = Async.AwaitTask packFileTask.Value
                    return
                        match packFile.TryFindHashOffset hash with
                        | ValueNone -> None
                        | ValueSome offset -> Some(struct (packFile.Path, offset))
                })
            return
                match searchResult with
                | Some(struct (path, offset)) -> Nullable({ PackFile = path; Offset = offset })
                | None -> Nullable()
        }
