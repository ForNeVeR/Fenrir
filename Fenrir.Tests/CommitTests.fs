module Fenrir.Tests.CommitTests

open System.IO
open System.Threading.Tasks
open Fenrir.Git
open Fenrir.Tests.TestUtils
open Xunit

[<Fact>]
let ``The program should parse commits properly``(): Task = task {
    let index = PackIndex TestDataRoot
    let! cmt = Commits.ReadCommit(index, TestDataRoot, "3cb4a57f644f322c852201a68d2211026912a228")
    Assert.Equal(cmt.Hash, "3cb4a57f644f322c852201a68d2211026912a228")
    Assert.Equal(cmt.Body.Tree, "25e78c44e06b1e5c9c9e39a6a827734eee784066")
    Assert.Equal(cmt.Body.Parents[1], "62f4d4ce40041cd6295eb4a3d663724b4952e7b5")
    Assert.Equal(cmt.Body.Parents[0], "c0573616ea63dba6c4b13398058b0950c33a524c")
}

[<Fact>]
let ``Program should change and find hash of parent tree in commit properly``(): Task = task {
    let index = PackIndex TestDataRoot
    let! cmt = Commits.ReadCommit(index, TestDataRoot, "3cb4a57f644f322c852201a68d2211026912a228")
    let newHash = "0000000000000000000000000000000000000000" |> Tools.stringToByte
    let newCmt = Commands.changeHashInCommit cmt.Body newHash
    Assert.Equal<byte>(newCmt.Tree |> Tools.stringToByte, newHash)
}

[<Fact>]
let ``Printing of parsed commit should not change the content``(): Task = task {
    let index = PackIndex TestDataRoot
    let! cmt = Commits.ReadCommit(index, TestDataRoot, "3cb4a57f644f322c852201a68d2211026912a228")
    use outputPrinted = Commands.commitBodyToStream cmt.Body |> Commands.doAndRewind

    let objectFilePath = TestDataRoot / "objects" / "3c" / "b4a57f644f322c852201a68d2211026912a228"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use tempStream = Zlib.unpackObject input |> Commands.doAndRewind
    use outputActual = new MemoryStream()
    Commands.guillotineObject tempStream outputActual |> ignore
    outputActual.Position <- 0L

    Assert.Equal<byte>(outputPrinted.ToArray(), outputActual.ToArray())
}
