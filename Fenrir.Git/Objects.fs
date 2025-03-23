// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// <summary>
/// Low-level functions related to Git as an
/// <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-Objects">object</a> storage.
/// </summary>
module Fenrir.Git.Objects

open System
open System.Globalization
open System.IO
open System.Text
open System.Threading.Tasks
open Fenrir.Git.Metadata
open Fenrir.Git.Packing
open Fenrir.Git.Tools
open Fenrir.Git.Zlib
open TruePath

/// <summary>Calculates a path to the designated object's file in the <c>.git</c> directory.</summary>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="objectHash">Hash of the object to seek.</param>
/// <remarks>
/// Note that this file is not guaranteed to exist even if an object exists in the repository — in case the object is
/// packed. To seek for both an object in both packed and non-packed form, use this function in combination with
/// <see cref="M:Fenrir.Git.Packing.ReadPackedObject(Fenrir.Git.PackIndex,Fenrir.Git.Sha1Hash)"/>.
/// </remarks>
let GetRawObjectPath(gitDirectory: LocalPath, objectHash: Sha1Hash): LocalPath =
    let objectHash = objectHash.ToString()
    gitDirectory / "objects" / objectHash.Substring(0, 2) / objectHash.Substring(2, 38)

/// Reads the object header from an object stream.
let ReadHeaderFromStream(input: Stream): ObjectHeader =
    let bF = new BinaryReader(input, Encoding.ASCII)

    let maxTypeNameLength = uint64 "commit".Length
    let typeArray = readWhile (fun b -> b <> byte ' ') maxTypeNameLength bF
    let tp =
        match typeArray with
        | "tree"B   -> GitObjectType.GitTree
        | "commit"B -> GitObjectType.GitCommit
        | "blob"B   -> GitObjectType.GitBlob
        | _         -> failwithf "Invalid Git object header"

    let maxLength = uint64 (string UInt64.MaxValue).Length
    let sizeArray = readWhile (fun b -> b <> 0uy) maxLength bF
    let sz = Convert.ToUInt64(Encoding.ASCII.GetString(sizeArray), CultureInfo.InvariantCulture)

    {Type = tp; Size = sz}

/// <summary>Writes the object header to the object stream.</summary>
/// <param name="type">Object type.</param>
/// <param name="size">Object size in bytes.</param>
/// <param name="output">Output stream to write to.</param>
let WriteHeader(``type``: GitObjectType, size: int64, output: Stream): unit =
    if size < 0 then failwithf $"Stream size is less than zero: {size}."
    match ``type`` with
    | GitObjectType.GitTree   -> output.Write(ReadOnlySpan<byte>("tree "B))
    | GitObjectType.GitCommit -> output.Write(ReadOnlySpan<byte>("commit "B))
    | GitObjectType.GitBlob   -> output.Write(ReadOnlySpan<byte>("blob "B))
    | _                       -> failwithf "Invalid type of Git object"
    output.Write(ReadOnlySpan<byte>(size.ToString(CultureInfo.InvariantCulture)
                                    |> Encoding.ASCII.GetBytes))
    output.WriteByte(00uy)

/// <summary>Reads the header of any object in the Git storage.</summary>
/// <param name="index">Git pack index to search the objects in.</param>
/// <param name="gitDirectory">Path to the repository's <c>.git</c> directory.</param>
/// <param name="objectHash">Hash of the object.</param>
let ReadHeader(index: PackIndex, gitDirectory: LocalPath, objectHash: Sha1Hash): Task<ObjectHeader> =
    let rawObjectPath = GetRawObjectPath(gitDirectory, objectHash)
    if File.Exists rawObjectPath.Value
    then
        use input = new FileStream(rawObjectPath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
        use decodedInput = input |> getDecodedStream
        Task.FromResult <| ReadHeaderFromStream decodedInput
    else task {
        let! packedObject = ReadPackedObject(index, objectHash)
        use po = nonNull packedObject

        return {
            Type = po.ObjectType
            Size = Checked.uint64 po.Stream.Length
        }
    }

/// <summary>Removes a header from an object, writes the result into the <paramref name="output"/> stream.</summary>
/// <param name="input">Stream with input object.</param>
/// <param name="output">Stream with output object.</param>
/// <returns>Written byte count.</returns>
let Guillotine(input: Stream, output: Stream): int =
    ReadHeaderFromStream input |> ignore
    let bR = new BinaryReader(input)
    let bW = new BinaryWriter(output)
    let rec rewrite n:int =
        try
            bW.Write(bR.ReadByte())
            rewrite (n + 1)
        with
            | :? EndOfStreamException -> n
    rewrite 0

/// <summary>Writes object header followed by the passed object to the output stream.</summary>
/// <param name="type">Object type.</param>
/// <param name="input">The body of the input object (no header).</param>
/// <param name="headed">The output stream to out the data into.</param>
/// <returns>Hash of the object.</returns>
let WriteObject(``type``: GitObjectType, input: Stream, headed: MemoryStream): Sha1Hash =
    WriteHeader(``type``, input.Length, headed)
    input.CopyTo headed
    headed.Position <- 0L
    let hash = Sha1.CalculateHardened headed
    headed.Position <- 0L
    hash

/// <summary>Writes a full object stream to a corresponding file in the repository.</summary>
/// <param name="gitDirectory">Path to the <c>.git</c> directory.</param>
/// <param name="object">Unpacked object content.</param>
/// <param name="hash">Object hash.</param>
let WriteToFile(gitDirectory: LocalPath, object: MemoryStream, hash: Sha1Hash): unit =
    let pathToDirectory = gitDirectory / "objects" / hash.ToString().Substring(0, 2)
    let pathToFile = GetRawObjectPath(gitDirectory, hash)
    match Directory.Exists pathToDirectory.Value with
        | true -> ()
        | false -> Directory.CreateDirectory pathToDirectory.Value |> ignore
    match File.Exists pathToFile.Value with
        | true -> ()
        | false ->
            use output = new FileStream(pathToFile.Value, FileMode.CreateNew, FileAccess.Write)
            PackObject(object, output)
