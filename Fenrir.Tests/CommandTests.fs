module Fenrir.Tests.CommandTests

open System.IO
open System.Text

open Xunit

open Fenrir
open Fenrir.Metadata
open Fenrir.Tests.TestUtils

[<Fact>]
let ``Deflate decompression should read the file properly``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    let actualObjectContents = "blob 10\x00Test file\n"
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = new MemoryStream()
    Zlib.unpackObject input output

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))

[<Fact>]
let ``Deflate compression should write the file properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B

    use input = new MemoryStream(actualObjectContents)
    use compressedOutput = Zlib.packObject input |> Commands.doAndRewind
    use unCompressedOutput = Zlib.unpackObject compressedOutput |> Commands.doAndRewind

    Assert.Equal<byte>(actualObjectContents, unCompressedOutput.ToArray())

[<Fact>]
let ``Blob object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Zlib.unpackObject input |> Commands.doAndRewind

    let header = Commands.readHeader output
    let actualHeader = { Type = GitObjectType.GitBlob; Size = 10UL }
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Tree object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "0ba2ef789f6245b6b6604f54706b1dce1d84907f")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Zlib.unpackObject input |> Commands.doAndRewind

    let header = Commands.readHeader output
    let actualHeader = { Type = GitObjectType.GitTree; Size = 63UL }
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Commit object header should be read``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "cc07136d669554cf46ca4e9ef1eab7361336e1c8")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use output = Zlib.unpackObject input |> Commands.doAndRewind

    let header = Commands.readHeader output
    let actualHeader = { Type = GitObjectType.GitCommit; Size = 242UL }
    Assert.Equal(actualHeader, header)

[<Fact>]
let ``Cutting off header should write file properly``(): unit =
    let objectFilePath = Path.Combine(testDataRoot, "524acfffa760fd0b8c1de7cf001f8dd348b399d8")
    let actualObjectContents = "Test file\n"
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use decodedInput = Zlib.unpackObject input |> Commands.doAndRewind
    use output = new MemoryStream()
    let n = Commands.guillotineObject decodedInput output

    Assert.Equal(actualObjectContents, Encoding.UTF8.GetString(output.ToArray()))
    Assert.Equal(n, actualObjectContents.Length)

[<Fact>]
let ``The program should parse commits properly``(): unit =
    let cmt = Commands.parseCommitBody testDataRoot "3cb4a57f644f322c852201a68d2211026912a228"
    Assert.Equal(cmt.Tree, "25e78c44e06b1e5c9c9e39a6a827734eee784066")
    Assert.Equal(cmt.Parents.[1], "62f4d4ce40041cd6295eb4a3d663724b4952e7b5")
    Assert.Equal(cmt.Parents.[0], "c0573616ea63dba6c4b13398058b0950c33a524c")

[<Fact>]
let ``The program should parse trees properly``(): unit =
    let tr = Commands.parseTreeBody testDataRoot "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let hashFile = "e2af08e76b2408a88f13d2c64ca89f2d03c98385" |> Tools.stringToByte
    let hashTree = "184b3cc0e467ff9ef8f8ad2fb0565ab06dfc2f05" |> Tools.stringToByte
    Assert.Equal(tr.Length, 2)
    Assert.Equal(tr.[0].Mode, 100644UL)
    Assert.Equal(tr.[0].Name, "README")
    Assert.Equal<byte>(tr.[0].Hash, hashFile)
    Assert.Equal(tr.[1].Mode, 40000UL)
    Assert.Equal(tr.[1].Name, "ex")
    Assert.Equal<byte>(tr.[1].Hash, hashTree)

[<Fact>]
let ``Hasher should calculate file name properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B
    let fileName = "524acfffa760fd0b8c1de7cf001f8dd348b399d8"

    use input = new MemoryStream(actualObjectContents)
    Assert.Equal(fileName, Commands.SHA1 input |> Tools.byteToString)

[<Fact>]
let ``Converting String to byte[] and backward should not change the String``(): unit =
    let fileName = "524acfffa760fd0b8c1de7cf001f8dd348b399d8"
    Assert.Equal(fileName, Tools.stringToByte fileName |> Tools.byteToString)

[<Fact>]
let ``Restoring head should work properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B
    use input = new MemoryStream(actualObjectContents)
    use cuttedInput = new MemoryStream()
    Commands.guillotineObject input cuttedInput |> ignore
    cuttedInput.Position <- 0L
    use output = new MemoryStream()
    Commands.headifyStream GitObjectType.GitBlob cuttedInput output |> ignore
    Assert.Equal<byte>(actualObjectContents, output.ToArray())

[<Fact>]
let ``Program should change and find hash of file in tree properly``(): unit =
    let tr = Commands.parseTreeBody testDataRoot "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let newHash = "0000000000000000000000000000000000000000" |> Tools.stringToByte
    let newTr = Commands.changeHashInTree tr newHash "README"
    Assert.Equal<byte>(Commands.hashOfObjectInTree newTr "README", newHash)

[<Fact>]
let ``Printing of parsed tree should not change the content``(): unit =
    let tr = Commands.parseTreeBody testDataRoot "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    use outputPrinted = Commands.treeBodyToStream tr |> Commands.doAndRewind

    let objectFilePath = Path.Combine(testDataRoot, "objects", "0b", "a2ef789f6245b6b6604f54706b1dce1d84907f")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use tempStream = Zlib.unpackObject input |> Commands.doAndRewind
    use outputActual = new MemoryStream()
    Commands.guillotineObject tempStream outputActual |> ignore
    outputActual.Position <- 0L

    Assert.Equal<byte>(outputPrinted.ToArray(), outputActual.ToArray())

[<Fact>]
let ``Program should change and find hash of parent tree in commit properly``(): unit =
    let cmt = Commands.parseCommitBody testDataRoot "3cb4a57f644f322c852201a68d2211026912a228"
    let newHash = "0000000000000000000000000000000000000000" |> Tools.stringToByte
    let newCmt = Commands.changeHashInCommit cmt newHash
    Assert.Equal<byte>(newCmt.Tree |> Tools.stringToByte, newHash)

[<Fact>]
let ``Printing of parsed commit should not change the content``(): unit =
    let cmt = Commands.parseCommitBody testDataRoot "3cb4a57f644f322c852201a68d2211026912a228"
    use outputPrinted = Commands.commitBodyToStream cmt |> Commands.doAndRewind

    let objectFilePath = Path.Combine(testDataRoot, "objects", "3c", "b4a57f644f322c852201a68d2211026912a228")
    use input = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)
    use tempStream = Zlib.unpackObject input |> Commands.doAndRewind
    use outputActual = new MemoryStream()
    Commands.guillotineObject tempStream outputActual |> ignore
    outputActual.Position <- 0L

    Assert.Equal<byte>(outputPrinted.ToArray(), outputActual.ToArray())

[<Fact>]
let ``updateObjectInTree should not change the whole tree if blob wasn't changed``(): unit =
    let parentHash = "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let subTreeHash = "184b3cc0e467ff9ef8f8ad2fb0565ab06dfc2f05"
    let oldBlobHash = "b5c9fc36bc435a3addb76b0115e8763c75eedf2a"
    let readmeHash = "e2af08e76b2408a88f13d2c64ca89f2d03c98385"

    let pathToFile = Path.Combine("ex", "FIGHTTHEMACHINE")
    use treeStreams = Commands.updateObjectInTree parentHash testDataRoot pathToFile oldBlobHash

    Assert.Equal(treeStreams.Hashes.[0], parentHash)
    Assert.Equal(treeStreams.Hashes.[1], subTreeHash)

    let tr = Commands.streamToTreeBody treeStreams.Streams.[0]
    let subTr = Commands.streamToTreeBody treeStreams.Streams.[1]

    Assert.Equal(tr.Length, 2)
    Assert.Equal(tr.[0].Mode, 100644UL)
    Assert.Equal(tr.[0].Name, "README")
    Assert.Equal<byte>(tr.[0].Hash, readmeHash |> Tools.stringToByte)
    Assert.Equal(tr.[1].Mode, 40000UL)
    Assert.Equal(tr.[1].Name, "ex")
    Assert.Equal<byte>(tr.[1].Hash, subTreeHash |> Tools.stringToByte)

    Assert.Equal(subTr.Length, 1)
    Assert.Equal(subTr.[0].Mode, 100644UL)
    Assert.Equal(subTr.[0].Name, "FIGHTTHEMACHINE")
    Assert.Equal<byte>(subTr.[0].Hash, oldBlobHash |> Tools.stringToByte)

[<Fact>]
let ``updateObjectInTree should change the whole tree properly``(): unit =
    let oldParentHash = "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let newParentHash = "a3ecc1b7fb40831db85596a4f6d2b5e0a1070292"
    let newSubTreeHash = "b6c6d6bca44755db41e85040189d86c0dbec691e"
    let newBlobHash = "724978a20d84133868886a8e580f59c6f8586733"
    let readmeHash = "e2af08e76b2408a88f13d2c64ca89f2d03c98385"

    let pathToFile = Path.Combine("ex", "FIGHTTHEMACHINE")
    use treeStreams = Commands.updateObjectInTree oldParentHash testDataRoot pathToFile newBlobHash

    Assert.Equal(treeStreams.Hashes.[0], newParentHash)
    Assert.Equal(treeStreams.Hashes.[1], newSubTreeHash)

    let tr = Commands.streamToTreeBody treeStreams.Streams.[0]
    let subTr = Commands.streamToTreeBody treeStreams.Streams.[1]

    Assert.Equal(tr.Length, 2)
    Assert.Equal(tr.[0].Mode, 100644UL)
    Assert.Equal(tr.[0].Name, "README")
    Assert.Equal<byte>(tr.[0].Hash, readmeHash |> Tools.stringToByte)
    Assert.Equal(tr.[1].Mode, 40000UL)
    Assert.Equal(tr.[1].Name, "ex")
    Assert.Equal<byte>(tr.[1].Hash, newSubTreeHash |> Tools.stringToByte)

    Assert.Equal(subTr.Length, 1)
    Assert.Equal(subTr.[0].Mode, 100644UL)
    Assert.Equal(subTr.[0].Name, "FIGHTTHEMACHINE")
    Assert.Equal<byte>(subTr.[0].Hash, newBlobHash |> Tools.stringToByte)

[<Fact>]
let ``Files should be written after updating of the whole tree``(): unit =
    let oldParentHash = "0ba2ef789f6245b6b6604f54706b1dce1d84907f"
    let newBlobHash = "724978a20d84133868886a8e580f59c6f8586733"
    let pathToParentTree = Path.Combine(testDataRoot, ".git", "objects", "a3", "ecc1b7fb40831db85596a4f6d2b5e0a1070292")
    let pathToSubTree = Path.Combine(testDataRoot, ".git", "objects", "b6", "c6d6bca44755db41e85040189d86c0dbec691e")

    let pathToFile = Path.Combine("ex", "FIGHTTHEMACHINE")
    use treeStreams = Commands.updateObjectInTree oldParentHash testDataRoot pathToFile newBlobHash
    Commands.writeTreeObjects testDataRoot treeStreams
    Assert.True(File.Exists(pathToParentTree))
    Assert.True(File.Exists(pathToSubTree))
    File.Delete(pathToParentTree)
    File.Delete(pathToSubTree)


[<Fact>]
let ``Init command should create empty git repository``(): unit =
    doInTempDirectory (fun tempFolderForTest ->
        Commands.createEmptyRepo tempFolderForTest
        let gitRepoPath = Path.Combine(tempFolderForTest, ".git")
        gitRepoPath |> Directory.Exists |> Assert.True

        Path.Combine(gitRepoPath, "HEAD") |> File.Exists |> Assert.True
        Path.Combine(gitRepoPath, "description") |> File.Exists |> Assert.True
        Path.Combine(gitRepoPath, "config") |> File.Exists |> Assert.True

        let headContent = Path.Combine(gitRepoPath, "HEAD") |> File.ReadAllLines
        Assert.Equal<string>(headContent, [|"ref: refs/heads/master"|] )

        let descriptionContent = Path.Combine(gitRepoPath, "description") |> File.ReadAllLines
        Assert.Equal<string>(descriptionContent, [|"Unnamed repository; edit this file 'description' to name the repository."|] )

        let configContent = Path.Combine(gitRepoPath, "config") |> File.ReadAllLines
        Assert.Equal<string>(configContent, [|
        "[core]"
        "\trepositoryformatversion = 0"
        "\tfilemode = false"
        "\tbare = false"
        "\tlogallrefupdates = true"
        "\tsymlinks = false"
        "\tignorecase = true"
        |])

        let hooksPath = Path.Combine(gitRepoPath, "hooks")
        hooksPath |> Directory.Exists |> Assert.True

        let refsPath = Path.Combine(gitRepoPath, "refs")
        refsPath |> Directory.Exists |> Assert.True

        Path.Combine(refsPath,"heads") |> Directory.Exists |> Assert.True
        Path.Combine(refsPath,"tags") |> Directory.Exists |> Assert.True


        let objectsPath = Path.Combine(gitRepoPath, "objects")
        objectsPath |> Directory.Exists |> Assert.True

        Path.Combine(objectsPath,"pack") |> Directory.Exists |> Assert.True
        Path.Combine(objectsPath,"info") |> Directory.Exists |> Assert.True

        let infoPath = Path.Combine(gitRepoPath, "info")
        infoPath |> Directory.Exists |> Assert.True

        Path.Combine(infoPath, "exclude") |> File.Exists |> Assert.True

        let excludeContent = Path.Combine(infoPath, "exclude") |> File.ReadAllLines
        Assert.Equal<string>(excludeContent, [|
        "# git ls-files --others --exclude-from=.git/info/exclude"
        "# Lines that start with '#' are comments."
        "# For a project mostly in C, the following would be a good set of"
        "# exclude patterns (uncomment them if you want to use them):"
        "# *.[oa]"
        "# *~"
        |])
    )
