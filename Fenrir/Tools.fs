module Fenrir.Tools

open System
open System.IO
open System.Globalization

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
