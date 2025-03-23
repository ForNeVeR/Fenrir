// SPDX-FileCopyrightText: 2021-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.Sha1Tests

open System
open System.IO
open System.Security.Cryptography
open Fenrir.Git
open Xunit
open Fenrir.Git.Sha1
open Fenrir.Tests.TestUtils

let calcHashByImplementation (data: byte array) =
    use ms = new MemoryStream(data)
    (CalculateHardened ms).ToString()

let calcBuiltinHash (data: byte array) =
    use sha1 = SHA1.Create()
    sha1.ComputeHash(data) |> Convert.ToHexStringLower

[<Fact>]
let ``Sha1 impl returns same hash as builtin`` () : unit =
    let bytes = [| 1uy; 2uy; 3uy; 4uy |]

    let expected = calcBuiltinHash bytes

    let actual = calcHashByImplementation bytes

    Assert.Equal(expected, actual)

[<Fact>]
let ``Simple sha1 vulnerable to shattered attack`` () : unit =
    let pdfsDir = ExecutingAssemblyDirectory / "Sha1CollisionData"

    let pdf1 = File.ReadAllBytes((pdfsDir / "shattered-1.pdf").Value)
    let pdf2 = File.ReadAllBytes((pdfsDir / "shattered-2.pdf").Value)

    let hash1 = calcBuiltinHash pdf1
    let hash2 = calcBuiltinHash pdf2

    Assert.Equal(hash1, hash2)

[<Fact>]
let ``Run shattered with collision fix`` () : unit =
    let pdfsDir = ExecutingAssemblyDirectory / "Sha1CollisionData"

    let pdf1 = File.ReadAllBytes((pdfsDir / "shattered-1.pdf").Value)
    let pdf2 = File.ReadAllBytes((pdfsDir / "shattered-2.pdf").Value)

    let hash1 = calcHashByImplementation pdf1
    let hash2 = calcHashByImplementation pdf2


    Assert.NotEqual<string>(hash1, hash2)
    Assert.Equal("16e96b70000dd1e7c85b8368ee197754400e58ec", hash1, true, false, false)
    Assert.Equal("e1761773e6a35916d99f891b77663e6405313587", hash2, true, false, false)

[<Fact>]
let ``Hasher should calculate file name properly``(): unit =
    let actualObjectContents = "blob 10\x00Test file\n"B
    let fileName = "524acfffa760fd0b8c1de7cf001f8dd348b399d8"

    use input = new MemoryStream(actualObjectContents)
    Assert.Equal(Sha1Hash.OfHexString fileName, CalculateHardened input)
