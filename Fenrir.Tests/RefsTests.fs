// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.RefsTests

open System.IO
open System.Threading.Tasks
open TruePath
open Xunit

open Fenrir.Git
open Fenrir.Tests.TestUtils

[<Fact>]
let ``Attached head should be recognized properly``(): unit =
    Assert.False(Refs.isHeadDetached(LocalPath "Data"))

[<Fact>]
let ``Detached head should be recognized properly``(): unit =
    Assert.True(Refs.isHeadDetached(LocalPath "Data2"))


[<Fact>]
let ``Ref list should be read properly``():unit =
    let refs = Refs.readRefs TestDataRoot
    let sha1 = Sha1Hash.OfHexString
    let expectedRefs = [|
        { Name = "refs/heads/feature/feature-name"
          CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" |> sha1 }
        { Name = "refs/heads/master"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" |> sha1 }
        { Name = "refs/remotes/origin/HEAD";  CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" |> sha1 }
        { Name = "refs/remotes/origin/master"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" |> sha1 }
        { Name = "refs/tags/apr2016"; CommitObjectId = "953b93514f32c580081b81be9e2918214e9891a2" |> sha1 }
        { Name = "refs/tags/apr2017"; CommitObjectId = "77969da813b975ff6b7805814bb3c959cbcc1d6c" |> sha1 }
        { Name = "refs/tags/apr2018"; CommitObjectId = "6ffb8ec42b04d0f9334c1799338a0fa73381ad3d" |> sha1 }

    |]
    Assert.Equal<Ref>(expectedRefs, refs)


[<Fact>]
let ``Refs should be identified properly``(): unit =
    let commitHash = "8871e454a771b34cd83feda3efd5ab4bf2e35783" |> Sha1Hash.OfHexString
    let refs = Refs.identifyRefs commitHash TestMoreDateRoot
    Assert.Equal(2, Seq.length refs)
    refs |> Seq.iter (fun item -> Assert.Equal(item.CommitObjectId, commitHash))

let private DoWithTestRepo(headFileContent: string, testBranchName: string | null, testBranchFileContent: string | null)
                          (check: AbsolutePath -> Task) = task {
    let testRepoBase = Temporary.CreateTempFolder()
    let testRepoGitDir = testRepoBase / ".git"
    try
        let headFile = testRepoGitDir / "HEAD"
        Directory.CreateDirectory(headFile.Parent.Value.Value) |> ignore
        do! File.WriteAllTextAsync(headFile.Value, headFileContent)

        match testBranchName with
        | null -> ()
        | testBranchName ->
            let testBranchFile = testRepoGitDir / testBranchName
            Directory.CreateDirectory(testBranchFile.Parent.Value.Value) |> ignore
            do! File.WriteAllTextAsync(testBranchFile.Value, testBranchFileContent)

        do! check testRepoGitDir
    finally
        Directory.Delete(testRepoGitDir.Value, recursive = true)
}

[<Fact>]
let ``Normal branch HEAD is read correctly``(): Task =
    DoWithTestRepo
        ("ref: refs/heads/main", "refs/heads/main", "7c650bc240cbeccbb347a7338e3dd83f3e2a0c62")
        (fun gitDir -> task {
            let! ref = Refs.ReadHeadRef(LocalPath gitDir)
            Assert.Equal(
                { Name = "refs/heads/main"
                  CommitObjectId = "7c650bc240cbeccbb347a7338e3dd83f3e2a0c62" |> Sha1Hash.OfHexString },
                nonNull ref
            )
        })

[<Fact>]
let ``Detached HEAD is read correctly``(): Task =
    DoWithTestRepo("7c650bc240cbeccbb347a7338e3dd83f3e2a0c62", null, null) (fun gitDir -> task {
        let! ref = Refs.ReadHeadRef(LocalPath gitDir)
        Assert.Equal(
            { Name = null; CommitObjectId = "7c650bc240cbeccbb347a7338e3dd83f3e2a0c62" |> Sha1Hash.OfHexString },
            nonNull ref
        )
    })

[<Fact>]
let ``Abnormal HEAD (extra spaces) is read correctly``(): Task =
    DoWithTestRepo
        ("ref:   refs/heads/main   ", "refs/heads/main", "7c650bc240cbeccbb347a7338e3dd83f3e2a0c62")
        (fun gitDir -> task {
            let! ref = Refs.ReadHeadRef(LocalPath gitDir)
            Assert.Equal(
                { Name = "refs/heads/main"
                  CommitObjectId = "7c650bc240cbeccbb347a7338e3dd83f3e2a0c62" |> Sha1Hash.OfHexString },
                nonNull ref
            )
        })
