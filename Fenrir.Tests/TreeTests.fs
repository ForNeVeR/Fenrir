// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.TreeTests

open System.IO
open System.Threading.Tasks
open Fenrir.Git
open Fenrir.Tests.TestUtils
open JetBrains.Lifetimes
open Xunit

[<Fact>]
let ``The program should parse trees properly``(): Task = task {
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! tr = Trees.ReadTreeBody(index, TestDataRoot, Sha1Hash.OfHexString "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    let hashFile = "e2af08e76b2408a88f13d2c64ca89f2d03c98385" |> Sha1Hash.OfHexString
    let hashTree = "184b3cc0e467ff9ef8f8ad2fb0565ab06dfc2f05" |> Sha1Hash.OfHexString
    Assert.Equal(tr.Length, 2)
    Assert.Equal(tr[0].Mode, 100644UL)
    Assert.Equal(tr[0].Name, "README")
    Assert.Equal(tr[0].Hash, hashFile)
    Assert.Equal(tr[1].Mode, 40000UL)
    Assert.Equal(tr[1].Name, "ex")
    Assert.Equal(tr[1].Hash, hashTree)
}

[<Fact>]
let ``Program should change and find hash of file in tree properly``(): Task = task {
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! tr = Trees.ReadTreeBody(index, TestDataRoot, Sha1Hash.OfHexString "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    let newHash = "0000000000000000000000000000000000000000" |> Sha1Hash.OfHexString
    let newTr = Trees.ChangeHashInTree tr newHash "README"
    Assert.Equal(Trees.HashOfObjectInTree(newTr, "README"), newHash)
}

[<Fact>]
let ``Printing of parsed tree should not change the content``(): Task = task {
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! tr = Trees.ReadTreeBody(index, TestDataRoot, Sha1Hash.OfHexString "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    use outputPrinted = Tools.doAndRewind(fun out -> Trees.WriteTreeBody(tr, out))

    let objectFilePath = TestDataRoot / "objects" / "0b" / "a2ef789f6245b6b6604f54706b1dce1d84907f"
    use input = new FileStream(objectFilePath.Value, FileMode.Open, FileAccess.Read, FileShare.Read)
    use tempStream = Tools.doAndRewind(fun out -> Zlib.UnpackObject(input, out))
    use outputActual = new MemoryStream()
    Objects.Guillotine(tempStream, outputActual) |> ignore
    outputActual.Position <- 0L

    Assert.Equal<byte>(outputPrinted.ToArray(), outputActual.ToArray())
}
