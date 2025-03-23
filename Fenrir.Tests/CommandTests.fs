// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.CommandTests

open System.IO

open System.Threading.Tasks
open JetBrains.Lifetimes
open Xunit

open Fenrir.Git
open Fenrir.Git.Metadata
open Fenrir.Tests.TestUtils

[<Fact>]
let ``UpdateObjectInTree should not change the whole tree if blob wasn't changed``(): Task = task {
    let parentHash = Sha1Hash.OfHexString "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let subTreeHash = Sha1Hash.OfHexString "184b3cc0e467ff9ef8f8ad2fb0565ab06dfc2f05"
    let oldBlobHash = Sha1Hash.OfHexString "b5c9fc36bc435a3addb76b0115e8763c75eedf2a"
    let readmeHash = "e2af08e76b2408a88f13d2c64ca89f2d03c98385"

    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let pathToFile = Path.Combine("ex", "FIGHTTHEMACHINE")
    let! treeStreams = Commands.UpdateObjectInTree(index, parentHash, TestDataRoot, pathToFile, oldBlobHash)
    use _ = treeStreams

    Assert.Equal(treeStreams.Hashes[0], parentHash)
    Assert.Equal(treeStreams.Hashes[1], subTreeHash)

    let tr = Trees.ParseTreeBody treeStreams.Streams[0]
    let subTr = Trees.ParseTreeBody treeStreams.Streams[1]

    Assert.Equal(tr.Length, 2)
    Assert.Equal(tr[0].Mode, 100644UL)
    Assert.Equal(tr[0].Name, "README")
    Assert.Equal(tr[0].Hash, readmeHash |> Sha1Hash.OfHexString)
    Assert.Equal(tr[1].Mode, 40000UL)
    Assert.Equal(tr[1].Name, "ex")
    Assert.Equal(tr[1].Hash, subTreeHash)

    Assert.Equal(subTr.Length, 1)
    Assert.Equal(subTr[0].Mode, 100644UL)
    Assert.Equal(subTr[0].Name, "FIGHTTHEMACHINE")
    Assert.Equal(subTr[0].Hash, oldBlobHash)
}

[<Fact>]
let ``UpdateObjectInTree should change the whole tree properly``(): Task = task {
    let oldParentHash = Sha1Hash.OfHexString "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let newParentHash = Sha1Hash.OfHexString "a3ecc1b7fb40831db85596a4f6d2b5e0a1070292"
    let newSubTreeHash = Sha1Hash.OfHexString "b6c6d6bca44755db41e85040189d86c0dbec691e"
    let newBlobHash = Sha1Hash.OfHexString "724978a20d84133868886a8e580f59c6f8586733"
    let readmeHash = "e2af08e76b2408a88f13d2c64ca89f2d03c98385"

    let pathToFile = Path.Combine("ex", "FIGHTTHEMACHINE")
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! treeStreams = Commands.UpdateObjectInTree(index, oldParentHash, TestDataRoot, pathToFile, newBlobHash)
    use _ = treeStreams

    Assert.Equal(treeStreams.Hashes[0], newParentHash)
    Assert.Equal(treeStreams.Hashes[1], newSubTreeHash)

    let tr = Trees.ParseTreeBody treeStreams.Streams[0]
    let subTr = Trees.ParseTreeBody treeStreams.Streams[1]

    Assert.Equal(tr.Length, 2)
    Assert.Equal(tr[0].Mode, 100644UL)
    Assert.Equal(tr[0].Name, "README")
    Assert.Equal(tr[0].Hash, readmeHash |> Sha1Hash.OfHexString)
    Assert.Equal(tr[1].Mode, 40000UL)
    Assert.Equal(tr[1].Name, "ex")
    Assert.Equal(tr[1].Hash, newSubTreeHash)

    Assert.Equal(subTr.Length, 1)
    Assert.Equal(subTr[0].Mode, 100644UL)
    Assert.Equal(subTr[0].Name, "FIGHTTHEMACHINE")
    Assert.Equal(subTr[0].Hash, newBlobHash)
}

[<Fact>]
let ``Files should be written after updating of the whole tree``(): Task = task {
    let oldParentHash = Sha1Hash.OfHexString "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let newBlobHash = Sha1Hash.OfHexString "724978a20d84133868886a8e580f59c6f8586733"
    let gitDirectory = TestDataRoot / ".git"
    let pathToParentTree = gitDirectory / "objects" / "a3" / "ecc1b7fb40831db85596a4f6d2b5e0a1070292"
    let pathToSubTree = gitDirectory / "objects" / "b6" / "c6d6bca44755db41e85040189d86c0dbec691e"

    let pathToFile = Path.Combine("ex", "FIGHTTHEMACHINE")
    use ld = new LifetimeDefinition()
    let index = PackIndex(ld.Lifetime, TestDataRoot)
    let! treeStreams = Commands.UpdateObjectInTree(index, oldParentHash, TestDataRoot, pathToFile, newBlobHash)
    use _ = treeStreams
    Commands.WriteTreeObjects(gitDirectory, treeStreams)
    Assert.True(File.Exists(pathToParentTree.Value))
    Assert.True(File.Exists(pathToSubTree.Value))
    File.Delete(pathToParentTree.Value)
    File.Delete(pathToSubTree.Value)
}

[<Fact>]
let ``Init command should create empty git repository``(): unit =
    DoInTempDirectory (fun tempFolderForTest ->
        Commands.InitializeRepository tempFolderForTest
        let gitRepoPath = tempFolderForTest / ".git"
        gitRepoPath.Value |> Directory.Exists |> Assert.True

        (gitRepoPath / "HEAD").Value |> File.Exists |> Assert.True
        (gitRepoPath / "description").Value |> File.Exists |> Assert.True
        (gitRepoPath / "config").Value |> File.Exists |> Assert.True

        let headContent = (gitRepoPath / "HEAD").Value |> File.ReadAllLines
        Assert.Equal<string>(headContent, [|"ref: refs/heads/master"|] )

        let descriptionContent = (gitRepoPath / "description").Value |> File.ReadAllLines
        Assert.Equal<string>(descriptionContent, [|"Unnamed repository; edit this file 'description' to name the repository."|] )

        let configContent = (gitRepoPath / "config").Value |> File.ReadAllLines
        Assert.Equal<string>(configContent, [|
        "[core]"
        "\trepositoryformatversion = 0"
        "\tfilemode = false"
        "\tbare = false"
        "\tlogallrefupdates = true"
        "\tsymlinks = false"
        "\tignorecase = true"
        |])

        let hooksPath = gitRepoPath / "hooks"
        hooksPath.Value |> Directory.Exists |> Assert.True

        let refsPath = gitRepoPath / "refs"
        refsPath.Value |> Directory.Exists |> Assert.True

        (refsPath / "heads").Value |> Directory.Exists |> Assert.True
        (refsPath / "tags").Value |> Directory.Exists |> Assert.True

        let objectsPath = gitRepoPath / "objects"
        objectsPath.Value |> Directory.Exists |> Assert.True

        (objectsPath / "pack").Value |> Directory.Exists |> Assert.True
        (objectsPath / "info").Value |> Directory.Exists |> Assert.True

        let infoPath = gitRepoPath / "info"
        infoPath.Value |> Directory.Exists |> Assert.True

        (infoPath / "exclude").Value |> File.Exists |> Assert.True

        let excludeContent = (infoPath / "exclude").Value |> File.ReadAllLines
        Assert.Equal<string>(excludeContent, [|
        "# git ls-files --others --exclude-from=.git/info/exclude"
        "# Lines that start with '#' are comments."
        "# For a project mostly in C, the following would be a good set of"
        "# exclude patterns (uncomment them if you want to use them):"
        "# *.[oa]"
        "# *~"
        |])
    )
