﻿module Fenrir.Tests.RefsTests

open Xunit

open Fenrir
open Fenrir.Tests.TestUtils

[<Fact>]
let ``Attached head should be recognized properly``(): unit =
    Assert.False(Refs.isHeadDetached("Data"))
    
[<Fact>]
let ``Detached head should be recognized properly``(): unit =
    Assert.True(Refs.isHeadDetached("moreData"))

[<Fact>]
let ``Ref list should be read properly``(): unit =
    let refs = Refs.readRefs testDataRoot
    let expectedRefs = [|
        { Name = "refs/heads/feature/feature-name"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
        { Name = "refs/heads/master"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
        { Name = "refs/remotes/origin/master"; CommitObjectId = "cc07136d669554cf46ca4e9ef1eab7361336e1c8" }
    |]
    Assert.Equal<Ref>(expectedRefs, refs)
