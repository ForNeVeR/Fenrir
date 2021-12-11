module Fenrir.Tests.Sha1Tests

open System
open System.IO
open System.Security.Cryptography
open Xunit
open Fenrir.Sha1
open Fenrir.Tests.TestUtils

let calcHashByImplementation (data: byte array) =
    use ms = new MemoryStream(data)
    calcSHA1Hash ms |> Convert.ToHexString

let calcBuiltinHash (data: byte array) =
    use sha1 = HashAlgorithm.Create("SHA1")
    sha1.ComputeHash(data) |> Convert.ToHexString

[<Fact>]
let ``Verify bad pack header signature should fail`` () : unit =
    let bytes = [| 1uy; 2uy; 3uy; 4uy |]

    let expected = calcBuiltinHash bytes

    let actual = calcHashByImplementation bytes

    Assert.Equal(expected, actual)

[<Fact>]
let ``Simple sha1 vulnerable to shattered attack`` () : unit =
    let pdfsDir =
        Path.Combine(executingAssemblyDirectory, "Sha1CollisionData")

    let pdf1 =
        File.ReadAllBytes(Path.Combine(pdfsDir, "shattered-1.pdf"))

    let pdf2 =
        File.ReadAllBytes(Path.Combine(pdfsDir, "shattered-2.pdf"))

    let hash1 = calcBuiltinHash pdf1
    let hash2 = calcBuiltinHash pdf2

    Assert.Equal(hash1, hash2)
