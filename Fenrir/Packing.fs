module Fenrir.packing

open System
open System.IO

let anotherEndian (reader: BinaryReader) : int =
    reader.ReadBytes 4
        |> Array.map (fun x -> int x)
        |> Array.reduce (fun x y ->
            x * 256 + y)

let readHash (reader: BinaryReader) : String =
    reader.ReadBytes 20
        |> Array.fold (fun acc elem ->
            String.Concat(acc, sprintf "%02x" elem)) ""

let parseIndex (path: String) (hash: String) : int =
    let sizePosition = 1028L
    let idxReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    idxReader.BaseStream.Position <- sizePosition

    let size = anotherEndian idxReader
    let hashes = Array.zeroCreate size
    seq{0..size - 1}
        |> Seq.iter (fun i ->
            hashes.[i] <- readHash idxReader)
    hashes.[0] |> printfn "%s"
    hashes.[1] |> printfn "%s"
    size
