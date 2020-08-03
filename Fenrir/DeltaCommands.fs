module Fenrir.DeltaCommands

open System.IO

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
    let copySize =
        extractBit comm.[3] +
        (extractBit comm.[2] <<< 8) +
        (extractBit comm.[1] <<< 16)

    nonDelta.BaseStream.Position <- int64 copyOffset
    mem.Write(nonDelta.ReadBytes(copySize), 0, copySize)
    counter
