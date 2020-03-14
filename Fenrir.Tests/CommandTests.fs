module Fenrir.Tests.CommandTests

open System.IO
open System.Text
open Xunit

open Fenrir
open Fenrir.Tests.TestUtils

[<Fact>]
let ``Deflate decompression should read the file properly``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    let actualObjectContents = "blob 10\x00Test file\n"
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = new MemoryStream()
    Commands.unpackObject input output

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))
