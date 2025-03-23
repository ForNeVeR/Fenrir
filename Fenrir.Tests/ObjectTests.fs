// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.ObjectTests

open System.IO
open System.Text
open Fenrir.Git
open Fenrir.Git.Metadata
open Fenrir.Tests.TestUtils
open Xunit

[<Fact>]
let ``Blob object header should be read``(): unit =
    let objectFilePath = TestDataRoot / "524acfffa760fd0b8c1de7cf001f8dd348b399d8"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Tools.doAndRewind(fun out -> Zlib.UnpackObject(input, out))

    let header = Objects.ReadHeaderFromStream output
    let actualHeader = { Type = GitObjectType.GitBlob; Size = 10UL }
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Tree object header should be read``(): unit =
    let objectFilePath = TestDataRoot / "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Tools.doAndRewind(fun out -> Zlib.UnpackObject(input, out))

    let header = Objects.ReadHeaderFromStream output
    let actualHeader = { Type = GitObjectType.GitTree; Size = 63UL }
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Commit object header should be read``(): unit =
    let objectFilePath = TestDataRoot / "cc07136d669554cf46ca4e9ef1eab7361336e1c8"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Tools.doAndRewind(fun out -> Zlib.UnpackObject(input, out))

    let header = Objects.ReadHeaderFromStream output
    let actualHeader = { Type = GitObjectType.GitCommit; Size = 242UL }
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Cutting off header should write file properly``(): unit =
    let objectFilePath = TestDataRoot / "524acfffa760fd0b8c1de7cf001f8dd348b399d8"
    let actualObjectContents = "Test file\n"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use decodedInput = Tools.doAndRewind(fun out -> Zlib.UnpackObject(input, out))
    use output = new MemoryStream()
    let n = Objects.Guillotine(decodedInput, output)

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))
    Assert.Equal(n, actualObjectContents.Length)

[<Fact>]
let ``Restoring head should work properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B
    use input = new MemoryStream(actualObjectContents)
    use cuttedInput = new MemoryStream()
    Objects.Guillotine(input, cuttedInput) |> ignore
    cuttedInput.Position <- 0L
    use output = new MemoryStream()
    Objects.WriteObject(GitObjectType.GitBlob, cuttedInput, output) |> ignore
    Assert.Equal<byte>(actualObjectContents, output.ToArray())
