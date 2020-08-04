module Fenrir.Tests.PackTests

open System
open System.IO
open Xunit

open Fenrir.Tests.TestUtils
open Fenrir.Packing

let packTest(hash: string) (objType: string) (source: string): unit =
    use content = getPackedStream testDataRoot hash objType
    let expected = File.ReadAllBytes(Path.Combine(testDataRoot, source))
    Assert.Equal<string>(expected |> toString, content.ToArray() |> toString)

[<Fact>]
let ``Packed commit should be extracted properly``(): unit =
    let commitHash = "8c3ecc6b9abdab719915046ce7e989715fde5f5b"
    packTest commitHash "commit" "commit"

[<Fact>]
let ``Packed tree should be extracted properly``(): unit =
    let treeHash = "8401dc8f768e38f83b4c2a5c73d5393b093f5e6c"
    packTest treeHash "tree" "tree"

[<Fact>]
let ``Packed blob should be extracted properly``(): unit =
    let blobHash = "25872623397735b9946b9e7ddaf48ea8292c6eb9"
    packTest blobHash "blob" "blob"

[<Fact>]
let ``Delta objects should be parsed and extracted properly``(): unit =
    let deltaObj = "d36f4ae4294cc19e91d8c8fab1fb816630a6d79b"
    packTest deltaObj "blob" "deltaObj"
