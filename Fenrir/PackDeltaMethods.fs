module Fenrir.DeltaCommands

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

let byteToBits (num: byte) : byte[] =
    let mutable number = num
    Array.init 8 (fun _ ->
        let result = number % 2uy
        number <- number >>> 1
        result)


let bitsToInt (bits: byte[]) =
    let mutable st = 1

    Array.foldBack (fun i acc ->
        let result = acc + int i * st
        st <- st * 2
        result) bits 0

let insertCommand (reader: BinaryReader) (mem: MemoryStream) (flag: byte) (counter: int) =
    mem.Write(reader.ReadBytes(int flag), 0, int flag)
    counter + int flag

let copyCommand (comm: byte array) (mem: MemoryStream)
    (delta: BinaryReader) (nonDelta: BinaryReader) (count: int) =

    let mutable counter = count
    let extractBit (flag: byte) =
        match flag with
            | 1uy ->
                counter <- counter + 1
                int <| delta.ReadByte()
            | _ -> 0

    let copyOffset =
        extractBit comm.[7] +
        (extractBit comm.[6] <<< 8) +
        (extractBit comm.[5] <<< 16) +
        (extractBit comm.[4] <<< 24)
    let mutable copySize =
        extractBit comm.[3] +
        (extractBit comm.[2] <<< 8) +
        (extractBit comm.[1] <<< 16)
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
            let comm = flagByte |> byteToBits |> Array.rev
            counter <- counter + 1
            match comm.[0] with
                    | 0uy ->
                        counter <- insertCommand deltaReader result flagByte counter
                    | 1uy ->
                        counter <- copyCommand comm result deltaReader nonDelta counter
                    | _ -> failwithf "0 or 1 expected, something in bitsExtractor went FUBAR"
            applyDelta()

    applyDelta()
    result
