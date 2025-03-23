// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.CommitTests

open System.IO
open System.Threading.Tasks
open Fenrir.Git
open Fenrir.Tests.TestUtils
open JetBrains.Lifetimes
open Xunit

[<Fact>]
let ``The program should parse commits properly``(): Task = task {
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! cmt = Commits.ReadCommit(index, TestDataRoot, "3cb4a57f644f322c852201a68d2211026912a228" |> Sha1Hash.OfHexString)
    Assert.Equal(cmt.Hash, "3cb4a57f644f322c852201a68d2211026912a228" |> Sha1Hash.OfHexString)
    Assert.Equal(cmt.Body.Tree, "25e78c44e06b1e5c9c9e39a6a827734eee784066" |> Sha1Hash.OfHexString)
    Assert.Equal(cmt.Body.Parents[1], "62f4d4ce40041cd6295eb4a3d663724b4952e7b5" |> Sha1Hash.OfHexString)
    Assert.Equal(cmt.Body.Parents[0], "c0573616ea63dba6c4b13398058b0950c33a524c" |> Sha1Hash.OfHexString)
}

[<Fact>]
let ``Program should change and find hash of parent tree in commit properly``(): Task = task {
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! cmt = Commits.ReadCommit(index, TestDataRoot, "3cb4a57f644f322c852201a68d2211026912a228" |> Sha1Hash.OfHexString)
    let newHash = "0000000000000000000000000000000000000000" |> Sha1Hash.OfHexString
    let newCmt = { cmt.Body with Tree = newHash }
    Assert.Equal(newCmt.Tree, newHash)
}

[<Fact>]
let ``Printing of parsed commit should not change the content``(): Task = task {
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! cmt = Commits.ReadCommit(index, TestDataRoot, "3cb4a57f644f322c852201a68d2211026912a228" |> Sha1Hash.OfHexString)
    use outputPrinted = Tools.doAndRewind(fun out -> Commits.WriteCommitBody(cmt.Body, out))

    let objectFilePath = TestDataRoot / "objects" / "3c" / "b4a57f644f322c852201a68d2211026912a228"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use tempStream = Tools.doAndRewind(fun out -> Zlib.UnpackObject(input, out))
    use outputActual = new MemoryStream()
    Objects.Guillotine(tempStream, outputActual) |> ignore
    outputActual.Position <- 0L

    Assert.Equal<byte>(outputPrinted.ToArray(), outputActual.ToArray())
}
