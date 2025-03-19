namespace Fenrir.Git

open System
open System.Threading.Tasks
open TruePath

[<Struct>]
type PackedObjectLocation = {
    PackFile: LocalPath
    Offset: uint32
}

/// <summary>
///     <para>Type to store, cache and access information about Git pack files.</para>
///     <para>This type is thread-safe.</para>
/// </summary>
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
///         repository). To target such scenarios, we allow to read and store the fanout tables of all the pack index
///         files at once.
///     </item>
/// </list>
/// </remarks>
type PackIndex(gitDir: LocalPath) =

    /// <summary>Searches an object with <paramref name="hash"/> among the pack files included in the index.</summary>
    /// <param name="hash">Hash of the searched object.</param>
    /// <returns>Path to a pack file containing the object.</returns>
    member _.FindPackOfObject(hash: string): Task<Nullable<PackedObjectLocation>> =
        task {
            return failwith "TODO"
        }
