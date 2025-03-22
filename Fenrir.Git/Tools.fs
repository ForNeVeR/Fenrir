// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module internal Fenrir.Git.Tools

open System
open System.IO
open System.Collections
open System.Runtime.InteropServices

let sliceBitArray (bits: BitArray) (start: int) (finish: int): BitArray =
    [| for b in bits do yield b |][start..finish] |> BitArray

let compareBitArrays (immut: BitArray) (mut: BitArray): bool =
    seq {for b in mut.Xor immut do yield b} |> Seq.exists (fun item -> item = true) |> not

let readWhile (condition: byte -> bool) (maxSize: uint64) (stream: BinaryReader): byte array =
    let rec makeList (n: uint64): byte list =
        let newByte = stream.ReadByte()
        match (condition newByte) with
            | _ when n > maxSize -> failwithf "Invalid Git object header"
            | true                 -> newByte :: makeList (n + 1UL)
            | false                -> []
    makeList(0UL) |> List.toArray

let readWhileLast (condition: byte -> bool) (maxSize: uint64) (stream: BinaryReader): byte array =
    let rec makeList (n: uint64): byte list =
        let newByte = stream.ReadByte()
        match (condition newByte) with
            | _ when n > maxSize -> failwithf "Invalid Git object header"
            | true                 -> newByte :: makeList (n + 1UL)
            | false                -> [newByte]
    makeList(0UL) |> List.toArray

type BinaryReader with
    member reader.ReadBigEndianInt() : int =
        reader.ReadInt32()
        |> Net.IPAddress.NetworkToHostOrder

    member reader.ReadHash(): Sha1Hash =
        reader.ReadBytes 20
        |> Sha1Hash.OfBytes

type UnmanagedMemoryStream with
    member internal stream.ReadBigEndianUInt32(): uint32 =
        let mutable result = 0
        let span = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(&result, 1))
        stream.ReadExactly(span)
        result |> Net.IPAddress.NetworkToHostOrder |> uint32
