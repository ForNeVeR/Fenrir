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

let parseIndexOffset (path: String) (hash: String) : int =
    let idxReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
    //skip header and fanout table
    idxReader.BaseStream.Position <- 1028L
    //last item in fanout table
    let size = anotherEndian idxReader
    //hashes extraction
    let hashes = Array.init size (fun _ -> readHash idxReader)
    //position binary search of the hash
    let pos = Array.BinarySearch(hashes, hash)
    //skipping crc table and getting offset location
    idxReader.BaseStream.Position <-
        idxReader.BaseStream.Position + int64 size * 4L
        + (int64 pos) * 4L

    anotherEndian idxReader
