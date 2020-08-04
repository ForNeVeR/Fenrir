module Fenrir.Tests.PackTests

open System.IO
open System.Text
open Xunit

open Fenrir.Tests.TestUtils
open Fenrir.Packing

[<Fact>]
let ``Packed commit should be parsed properly``(): unit =
    let toString (arr: byte array) =
        arr |> Encoding.ASCII.GetString

    let commitHash = "8c3ecc6b9abdab719915046ce7e989715fde5f5b"
    use content = getPackedStream testDataRoot commitHash "commit"
    let expected = File.ReadAllBytes(Path.Combine(testDataRoot, "commit"))
    Assert.Equal<string>(expected |> toString, content.ToArray() |> toString)
