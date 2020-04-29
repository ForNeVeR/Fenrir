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

[<Fact>]
let ``Blob object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = new MemoryStream()
    Commands.unpackObject input output
    output.Position <- 0L

    let header = Commands.readHeader output
    let actualHeader = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitBlob; Commands.ObjectHeader.Size = 10UL}
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Tree object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = new MemoryStream()
    Commands.unpackObject input output
    output.Position <- 0L

    let header = Commands.readHeader output
    let actualHeader = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitTree; Commands.ObjectHeader.Size = 63UL}
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Commit object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "cc07136d669554cf46ca4e9ef1eab7361336e1c8")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = new MemoryStream()
    Commands.unpackObject input output
    output.Position <- 0L

    let header = Commands.readHeader output
    let actualHeader = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitCommit; Commands.ObjectHeader.Size = 242UL}
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Cutting off header should write file properly``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    let actualObjectContents = "Test file\n"
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use decodedInput = new MemoryStream()
    Commands.unpackObject input decodedInput
    decodedInput.Position <- 0L
    use output = new MemoryStream()
    let n = Commands.guillotineObject decodedInput output

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))
    Assert.Equal(n, actualObjectContents.Length)

[<Fact>]
let ``The program should find branch list properly``(): unit =
    let ff = Commands.readBranchList(testDataRoot)
    Assert.Equal(fst ff.[0], Path.Combine(testDataRoot, ".git", "refs", "heads", "master"))
    Assert.Equal(snd ff.[0], "cc07136d669554cf46ca4e9ef1eab7361336e1c8")

[<Fact>]
let ``The program should parse commits properly``(): unit =
    let cmt = Commands.parseCommitBody testDataRoot "3cb4a57f644f322c852201a68d2211026912a228"
    Assert.Equal(cmt.Tree, "25e78c44e06b1e5c9c9e39a6a827734eee784066")
    Assert.Equal(cmt.Parents.[0], "62f4d4ce40041cd6295eb4a3d663724b4952e7b5")
    Assert.Equal(cmt.Parents.[1], "c0573616ea63dba6c4b13398058b0950c33a524c")
