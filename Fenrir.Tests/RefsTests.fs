// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.RefsTests

open Xunit

open Fenrir
open Fenrir.Tests.TestUtils

[<Fact>]
let ``Attached head should be recognized properly``(): unit =
    Assert.False(Refs.isHeadDetached("Data"))

[<Fact>]
let ``Detached head should be recognized properly``(): unit =
    Assert.True(Refs.isHeadDetached("Data2"))

    
[<Fact>]
let ``Ref list should be read properly``():unit =
    let refs = Refs.readRefs testDataRoot
    let expectedRefs = [|
        { Name = "refs/heads/feature/feature-name"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
        { Name = "refs/heads/master"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
        { Name = "refs/remotes/origin/HEAD";  CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
        { Name = "refs/remotes/origin/master"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
        { Name = "refs/tags/apr2016"; CommitObjectId = "953b93514f32c580081b81be9e2918214e9891a2" }
        { Name = "refs/tags/apr2017"; CommitObjectId = "77969da813b975ff6b7805814bb3c959cbcc1d6c" }
        { Name = "refs/tags/apr2018"; CommitObjectId = "6ffb8ec42b04d0f9334c1799338a0fa73381ad3d" }

    |]
    Assert.Equal<Ref>(expectedRefs, refs)
    

[<Fact>]
let ``Refs should be identified properly``(): unit =
    let commitHash = "8871e454a771b34cd83feda3efd5ab4bf2e35783"
    let refs = Refs.identifyRefs commitHash testMoreDateRoot
    Assert.Equal(2, Seq.length refs)
    refs |> Seq.iter (fun item -> Assert.Equal<string>(item.CommitObjectId, commitHash))
