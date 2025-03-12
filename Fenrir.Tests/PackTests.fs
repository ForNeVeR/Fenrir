module Fenrir.Tests.PackTests

open System.IO
open System.Text
open Xunit

open Fenrir.Tests.TestUtils
open Fenrir.Packing

let private packTest (bom: bool) (hash: string) (source: string): unit =
    // Some of our test data objects include UTF-8 BOM due to historical reasons, while actual text has been cleaned up.
    // Work around that by appending BOM if required.
    // If you ever update the test data objects, please drop the BOM from them altogether.
    use packedObject = getPackedObject testDataRoot hash
    let expectedBytes = [|
        if bom then yield! [| 0xEFuy; 0xBBuy; 0xBFuy |] // UTF-8 BOM
        yield! File.ReadAllBytes <| Path.Combine(testDataRoot, source)
    |]
    let expected = expectedBytes |> toString
    let actual = packedObject.Stream.ToArray() |> toString
    Assert.Equal(expected, actual)

[<Fact>]
let ``Packed commit should be extracted properly``(): unit =
    let commitHash = "8c3ecc6b9abdab719915046ce7e989715fde5f5b"
    packTest false commitHash "commit"

[<Fact>]
let ``Packed tree should be extracted properly``(): unit =
    let treeHash = "8401dc8f768e38f83b4c2a5c73d5393b093f5e6c"
    packTest false treeHash "tree"

[<Fact>]
let ``Packed blob should be extracted properly``(): unit =
    let blobHash = "25872623397735b9946b9e7ddaf48ea8292c6eb9"
    packTest true blobHash "blob"

[<Fact>]
let ``Delta objects should be parsed and extracted properly``(): unit =
    let deltaObj = "d36f4ae4294cc19e91d8c8fab1fb816630a6d79b"
    packTest true deltaObj "deltaObj"
