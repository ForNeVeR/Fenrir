// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tools

open System
open System.IO
open System.Globalization
open System.Collections

let sliceBitArray (bits: BitArray) (start: int) (finish: int): BitArray =
    [| for b in bits do yield b |].[start..finish] |> BitArray

let compareBitArrays (immut: BitArray) (mut: BitArray): bool =
    seq {for b in mut.Xor immut do yield b} |> Seq.exists (fun item -> item = true) |> not

let readWhile (condition: byte -> bool) (maxSize: uint64) (stream: BinaryReader): byte array =
    let rec makeList (n: uint64): byte list =
        let newByte = stream.ReadByte()
        match (condition newByte) with
            | _ when n > (maxSize) -> failwithf "Invalid Git object header"
            | true                 -> newByte :: makeList (n + 1UL)
            | false                -> []
    makeList(0UL) |> List.toArray

let readWhileLast (condition: byte -> bool) (maxSize: uint64) (stream: BinaryReader): byte array =
    let rec makeList (n: uint64): byte list =
        let newByte = stream.ReadByte()
        match (condition newByte) with
            | _ when n > (maxSize) -> failwithf "Invalid Git object header"
            | true                 -> newByte :: makeList (n + 1UL)
            | false                -> [newByte]
    makeList(0UL) |> List.toArray

let byteToString (b : byte[]): String =
    BitConverter.ToString(b).Replace("-", "").ToLower()

let stringToByte (s: String): byte[] =
    match s.Length with
    | even when even % 2 = 0 ->
        let arrayLength = s.Length / 2
        Array.init arrayLength (fun byteIndex ->
            let charIndex = byteIndex * 2
            Byte.Parse(s.AsSpan(charIndex, 2), NumberStyles.AllowHexSpecifier, provider = CultureInfo.InvariantCulture)
        )
    | n -> failwithf "String of invalid length %d: %s" n s

type BinaryReader with
    member reader.ReadBigEndianInt() : int =
        reader.ReadInt32()
        |> Net.IPAddress.NetworkToHostOrder

    member reader.ReadHash() : String =
        reader.ReadBytes 20
        |> Array.fold (fun acc elem -> String.Concat(acc, $"%02x{elem}")) ""
