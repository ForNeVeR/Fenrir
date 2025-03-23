// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// <summary>
/// Functions to work with Git <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-Objects">tree objects</a>.
/// </summary>
module Fenrir.Git.Trees

open System
open System.Globalization
open System.IO
open System.Text
open System.Threading.Tasks
open Fenrir.Git.Metadata
open Fenrir.Git.Tools
open Fenrir.Git.Zlib
open TruePath

let private GetHeadlessTreeBody(size: uint64, decodedInput: Stream): TreeBody =
    let bF = new BinaryReader(decodedInput, Encoding.ASCII)
    let rec makeList (n:int): TreeAtom list =
        try
            {
                Mode = readWhile (fun b -> b <> byte ' ') size bF |> Encoding.ASCII.GetString |> Convert.ToUInt64
                Name = readWhile (fun b -> b <> 0uy) size bF |> Encoding.ASCII.GetString
                Hash = Sha1Hash.OfBytes <| bF.ReadBytes(20)
            } :: makeList (n + 1)
        with
            | :? EndOfStreamException -> []
    makeList 0 |> Array.ofList

/// <summary>Parses a tree body from the full object stream.</summary>
/// <param name="inputObject">The input object stream.</param>
/// <returns>The parsed tree body object.</returns>
let internal ParseTreeBody(inputObject: Stream): TreeBody =
    let hd = Objects.ReadHeaderFromStream inputObject
    match hd.Type with
        | GitObjectType.GitCommit   -> failwithf "Found commit file instead of tree file"
        | GitObjectType.GitBlob     -> failwithf "Found blob file instead of tree file"
        | GitObjectType.GitTree     -> GetHeadlessTreeBody(hd.Size, inputObject)
        | x -> failwithf $"Unknown Git object type: {x}."

/// <summary>Reads a tree object information.</summary>
/// <param name="index">Git pack index to search the objects in.</param>
/// <param name="path">Path to a repository's <c>.git</c> folder.</param>
/// <param name="hash">Hash of the tree object.</param>
/// <returns>A parsed tree body object.</returns>
let ReadTreeBody(index: PackIndex, path: LocalPath, hash: Sha1Hash): Task<TreeBody> =
    let pathToFile = Objects.GetRawObjectPath(path, hash)
    match File.Exists pathToFile.Value with
        | true ->
            use input = new FileStream(pathToFile.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
            use decodedInput = input |> getDecodedStream
            Task.FromResult(decodedInput |> ParseTreeBody)
        | false -> task {
            let! packedObject = Packing.ReadPackedObject(index, hash)
            use po = nonNull packedObject
            return GetHeadlessTreeBody(uint64 po.Stream.Length, po.Stream)
        }

/// Will find a named object in the tree body and return its hash.
let internal HashOfObjectInTree(tree: TreeBody, name: String): Sha1Hash =
    let atom = Array.find (fun a -> a.Name = name) tree
    atom.Hash

let internal ChangeHashInTree (tree: TreeBody) (hash: Sha1Hash) (name: String): TreeBody =
    let changer (a: TreeAtom) : TreeAtom =
        match (a.Name = name) with
            | true -> {Mode = a.Mode; Name = a.Name; Hash = hash}
            | false -> a
    Array.map changer tree

/// Writes a tree body to a stream.
let WriteTreeBody(tree: TreeBody, stream: Stream): unit =
    let printAtom (a : TreeAtom): unit =
        stream.Write(ReadOnlySpan<byte>(a.Mode.ToString(CultureInfo.InvariantCulture) |> Encoding.ASCII.GetBytes))
        stream.WriteByte(' 'B)
        stream.Write(ReadOnlySpan<byte>(a.Name |> Encoding.ASCII.GetBytes))
        stream.WriteByte(00uy)
        stream.Write(a.Hash.ToBytes())
    Array.iter printAtom tree
