// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.ZlibTests

open System.IO
open System.Text
open Fenrir.Git
open Fenrir.Tests.TestUtils
open Xunit

[<Fact>]
let ``Deflate decompression should read the file properly``(): unit =
    let objectFilePath = TestDataRoot / "524acfffa760fd0b8c1de7cf001f8dd348b399d8"
    let actualObjectContents = "blob 10\x00Test file\n"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = new MemoryStream()
    Zlib.unpackObject input output

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))

[<Fact>]
let ``Deflate compression should write the file properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B

    use input = new MemoryStream(actualObjectContents)
    use compressedOutput = Zlib.packObject input |> Tools.doAndRewind
    use unCompressedOutput = Zlib.unpackObject compressedOutput |> Tools.doAndRewind

    Assert.Equal<byte>(actualObjectContents, unCompressedOutput.ToArray())
