module Fenrir.Tests.CommandTests

open System.IO
open System.Text
open Xunit

open System.Linq

open System
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

[<Fact>]
let ``Deflate compression should write the file properly``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    let actualObjectContents = "blob 10\x00Test file\n"B
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use memInput = input.CopyTo |> Commands.doAndRewind
    let byteArrayOne = memInput.ToArray()

    use output = new MemoryStream(actualObjectContents)
    output.Position <- 0L

    let newObjectFilePath = Path.Combine(testDataRoot, "testBlob")
    use codedOutput = new FileStream(newObjectFilePath, FileMode.Create, FileAccess.Write)
    Commands.packObject output codedOutput

    use newInput = new FileStream(newObjectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use newMemInput = newInput.CopyTo |> Commands.doAndRewind
    let byteArrayTwo = memInput.ToArray()

    Assert.True(byteArrayOne.SequenceEqual(byteArrayTwo))

[<Fact>]
let ``Blob object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Commands.unpackObject input |> Commands.doAndRewind

    let header = Commands.readHeader output
    let actualHeader = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitBlob; Commands.ObjectHeader.Size = 10UL}
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Tree object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Commands.unpackObject input |> Commands.doAndRewind

    let header = Commands.readHeader output
    let actualHeader = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitTree; Commands.ObjectHeader.Size = 63UL}
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Commit object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "cc07136d669554cf46ca4e9ef1eab7361336e1c8")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Commands.unpackObject input |> Commands.doAndRewind

    let header = Commands.readHeader output
    let actualHeader = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitCommit; Commands.ObjectHeader.Size = 242UL}
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Cutting off header should write file properly``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    let actualObjectContents = "Test file\n"
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use decodedInput = Commands.unpackObject input |> Commands.doAndRewind
    use output = new MemoryStream()
    let n = Commands.guillotineObject decodedInput output

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))
    Assert.Equal(n, actualObjectContents.Length)

[<Fact>]
let ``The program should find branch list properly``(): unit =
    let ff = Commands.readBranchList(testDataRoot)
    Assert.Equal(fst ff.[0], Path.Combine(testDataRoot, "refs", "heads", "master"))
    Assert.Equal(snd ff.[0], "cc07136d669554cf46ca4e9ef1eab7361336e1c8")


[<Fact>]
let ``The program should parse commits properly``(): unit =
    let cmt = Commands.parseCommitBody testDataRoot "3cb4a57f644f322c852201a68d2211026912a228"
    Assert.Equal(cmt.Tree, "25e78c44e06b1e5c9c9e39a6a827734eee784066")
    Assert.Equal(cmt.Parents.[0], "62f4d4ce40041cd6295eb4a3d663724b4952e7b5")
    Assert.Equal(cmt.Parents.[1], "c0573616ea63dba6c4b13398058b0950c33a524c")

[<Fact>]
let ``Hasher should calculate file name properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B
    let fileName = "524acfffa760fd0b8c1de7cf001f8dd348b399d8"

    use input = new MemoryStream(actualObjectContents)
    Assert.Equal(fileName, Commands.SHA1 input |> Commands.byteToString)

[<Fact>]
let ``Converting String to byte[] and backward should not change the String``(): unit =
    let fileName = "524acfffa760fd0b8c1de7cf001f8dd348b399d8"
    Console.Write (fileName)
    Assert.Equal(fileName, Commands.stringToByte fileName |> Commands.byteToString)

[<Fact>]
let ``Restoring head should work properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B
    use input = new MemoryStream(actualObjectContents)
    use cuttedInput = new MemoryStream()
    Commands.guillotineObject input cuttedInput |> ignore
    cuttedInput.Position <- 0L
    use output = new MemoryStream()
    Commands.writeObjectHeader Commands.GitObjectType.GitBlob cuttedInput output
    cuttedInput.CopyTo output
    output.Position <- 0L

    Assert.True(actualObjectContents.SequenceEqual(output.ToArray()))
