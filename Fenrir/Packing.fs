module Fenrir.packing

open System
open System.IO
open System.Text

let anotherEndian (reader: BinaryReader) : uint32 =
    let bytes = reader.ReadBytes 4 |> Array.map (fun x -> uint32 x)
    bytes |> Array.reduce (fun x y -> x * (uint32 256) + y)

let parseIndex (path: String) (hash: String) : uint32 =
    let idxReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    idxReader.BaseStream.Position <- 1028L
    anotherEndian idxReader
