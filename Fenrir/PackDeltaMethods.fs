module Fenrir.DeltaCommands

open System.Collections
open System.IO
open Fenrir.Tools
open Fenrir.Zlib

//checks if MSB (a.k.a. Most Significant Byte) is set to 1
let isMsbSet (b: byte): bool =
    b >= 128uy

let estimateSize (startSize: int) (off: int) (bytes: byte array): int =
        let mutable shift = off - 7
        Array.fold (fun acc elem ->
            shift <- shift + 7
            int (elem % 128uy) <<< shift ||| acc) startSize bytes

let byteToBits (num: byte) : BitArray =
    BitArray([| num |])



let bitsToInt (bits: BitArray): int =
    let mutable st = 1

    seq { for b in bits do yield b }
    |> Seq.fold (fun acc i ->
        let result = acc + (if i then 1 else 0) * st
        st <- st * 2
        result) 0

let insertCommand (reader: BinaryReader) (mem: MemoryStream) (flag: byte) (counter: int): int =
    mem.Write(reader.ReadBytes(int flag), 0, int flag)
    counter + int flag

let copyCommand (comm: BitArray) (mem: MemoryStream)
    (delta: BinaryReader) (nonDelta: BinaryReader) (count: int): int =

    let mutable counter = count
    let extractBit (flag: bool): int =
        match flag with
            | true ->
                counter <- counter + 1
                int <| delta.ReadByte()
            | false -> 0

    let copyOffset =
        extractBit comm.[0] +
        (extractBit comm.[1] <<< 8) +
        (extractBit comm.[2] <<< 16) +
        (extractBit comm.[3] <<< 24)
    let mutable copySize =
        extractBit comm.[4] +
        (extractBit comm.[5] <<< 8) +
        (extractBit comm.[6] <<< 16)
    if copySize = 0 then copySize <- 0x10000

    nonDelta.BaseStream.Position <- int64 copyOffset
    mem.Write(nonDelta.ReadBytes(copySize), 0, copySize)
    counter

let processDelta (pack: BinaryReader) (nonDelta: BinaryReader) (size: int): MemoryStream =
    use deltaReader = new BinaryReader(new MemoryStream(size + 20 |> pack.ReadBytes)
                                           |> getDecodedStream)
    let sourceSize = readWhileLast isMsbSet (uint64 deltaReader.BaseStream.Length) deltaReader
                         |> estimateSize 0 0
    let targetSize = readWhileLast isMsbSet (uint64 deltaReader.BaseStream.Length) deltaReader
                         |> estimateSize 0 0

    let deltaCommSize = size - (int) deltaReader.BaseStream.Position
    let result = new MemoryStream()
    let mutable counter = 0

    let rec applyDelta _ =
        let mutable flagByte = 0uy
        if counter < deltaCommSize then
            flagByte <- deltaReader.ReadByte()
            let comm = flagByte |> byteToBits
            counter <- counter + 1
            match comm.[7] with
                    | false ->
                        counter <- insertCommand deltaReader result flagByte counter
                    | true ->
                        counter <- copyCommand comm result deltaReader nonDelta counter
            applyDelta()

    applyDelta()
    result
