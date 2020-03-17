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
let ``Check headers``(): unit =
    // blob test:
    let objectFilePathBlob = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    use inputBlob = new FileStream(objectFilePathBlob, FileMode.Open, FileAccess.Read, FileShare.Read)
    use outputBlob = new MemoryStream()
    Commands.unpackObject inputBlob outputBlob
    outputBlob.Position <- 0L

    let headerBlob = Commands.readHeader outputBlob
    let actualHeaderBlob = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitBlob; Commands.ObjectHeader.Size = 10UL}
    Assert.Equal(headerBlob, actualHeaderBlob)

    // tree test:
    let objectFilePathTree = Path.Combine(testDataRoot, "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    use inputTree = new FileStream(objectFilePathTree, FileMode.Open, FileAccess.Read, FileShare.Read)
    use outputTree = new MemoryStream()
    Commands.unpackObject inputTree outputTree
    outputTree.Position <- 0L

    let headerTree = Commands.readHeader outputTree
    let actualHeaderTree = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitTree; Commands.ObjectHeader.Size = 63UL}
    Assert.Equal(headerTree, actualHeaderTree)

    // commit test:

    let objectFilePathCommit = Path.Combine(testDataRoot, "cc07136d669554cf46ca4e9ef1eab7361336e1c8")
    use inputCommit = new FileStream(objectFilePathCommit, FileMode.Open, FileAccess.Read, FileShare.Read)
    use outputCommit = new MemoryStream()
    Commands.unpackObject inputCommit outputCommit
    outputCommit.Position <- 0L

    let headerCommit = Commands.readHeader outputCommit
    let actualHeaderCommit = {Commands.ObjectHeader.Type = Commands.GitObjectType.GitCommit; Commands.ObjectHeader.Size = 242UL}
    Assert.Equal(headerCommit, actualHeaderCommit)
