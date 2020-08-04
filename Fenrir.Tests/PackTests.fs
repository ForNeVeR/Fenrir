module Fenrir.Tests.PackTests

open System
open System.IO
open Xunit

open Fenrir.Tests.TestUtils
open Fenrir.Packing

[<Fact>]
let ``Packed commit should be extracted properly``(): unit =
    let commitHash = "8c3ecc6b9abdab719915046ce7e989715fde5f5b"
    use content = getPackedStream testDataRoot commitHash "commit"
    let expected = File.ReadAllBytes(Path.Combine(testDataRoot, "commit"))
    Assert.Equal<string>(expected |> toString, content.ToArray() |> toString)

[<Fact>]
let ``Packed tree should be extracted properly``(): unit =
    let treeHash = "8401dc8f768e38f83b4c2a5c73d5393b093f5e6c"
    use content = getPackedStream testDataRoot treeHash "tree"
    let expected = File.ReadAllBytes(Path.Combine(testDataRoot, "tree"))
    Assert.Equal<string>(expected |> toString, content.ToArray() |> toString)

[<Fact>]
let ``Packed blob should be extracted properly``(): unit =
    let blobHash = "25872623397735b9946b9e7ddaf48ea8292c6eb9"
    use content = getPackedStream testDataRoot blobHash "blob"
    let expected = File.ReadAllBytes(Path.Combine(testDataRoot, "blob"))
    Assert.Equal<string>(expected |> toString, content.ToArray() |> toString)
