// SPDX-FileCopyrightText: 2021-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.VerifyPackTests

open System
open System.IO
open Fenrir.PackVerification
open Xunit

open Fenrir.Tests.TestUtils

type GetReaderInput =
    | Bytes of byte array array
    | Path of string

let getReader (input: GetReaderInput) : BinaryReader =
    match input with
    | Bytes bytes -> new BinaryReader(new MemoryStream(Array.concat bytes))
    | Path path -> new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))

let packPath =
    Path.Combine(testDataRoot, "objects", "pack", "pack-07a2498901f9e70c9056ae89d11aba80b3402616.pack")

[<Fact>]
let ``Verify bad pack header signature should fail`` () : unit =
    use reader = Bytes [| "RANDOM"B |] |> getReader

    let unitFunc =
        fun _ -> verifyPackHeader reader |> ignore

    Assert.Throws(unitFunc) |> ignore

[<Fact>]
let ``Verify pack header with wrong version should fail`` () : unit =
    use reader =
        Bytes [| "PACK"B
                 [| 0uy; 0uy; 0uy; 1uy |] |]
        |> getReader

    let unitFunc =
        fun _ -> verifyPackHeader reader |> ignore

    Assert.Throws(unitFunc) |> ignore

[<Fact>]
let ``Verify pack header should return object count`` () : unit =
    use reader = Path packPath |> getReader

    Assert.Equal(460, verifyPackHeader reader)

[<Fact>]
let ``Packed commit should be in pack`` () : unit =
    let commitHash =
        "99cc0056090374b2de8da965f58d5197b0b0f259"

    use reader = Path packPath |> getReader

    let objectsCount = verifyPackHeader reader
    let objects = parseObjects reader objectsCount

    Assert.True(
        (List.findIndex (fun (o: VerifyPackObjectInfo) -> o.Hash = commitHash) objects)
        <> -1
    )

[<Fact>]
let ``Count depth histogram should be correct`` () : unit =
    use reader = Path packPath |> getReader

    let objectsCount = verifyPackHeader reader
    let objects = parseObjects reader objectsCount
    let depths = calcDepthDistribution objects

    Assert.Equal(213, depths.Item 0)

[<Fact>]
let ``Verbose output equals get verify-pack -v out`` () : unit =
    use reader = Path packPath |> getReader

    let strings = verifyPack reader true |> Seq.toArray
    let actual = String.Join("\n", strings)

    let expected = File.ReadAllText(Path.Combine(testDataRoot, "verify-pack-v_output.txt"))
    Console.WriteLine expected
    Assert.Equal(actual, expected)
