// SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.Sha1HashTests

open System
open Fenrir.Git
open Xunit

[<Fact>]
let ``OfBytes should work as expected``(): unit =
    let bytes = [|
        0x00uy; 0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy; 0x06uy; 0x07uy; 0x08uy; 0x09uy;
        0x0Auy; 0x0Buy; 0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; 0x10uy; 0x11uy; 0x12uy; 0x13uy
    |]
    let hash = Sha1Hash.OfBytes bytes
    Assert.Equal({
        Byte0 = 0x00uy
        Byte1 = 0x01uy
        Byte2 = 0x02uy
        Byte3 = 0x03uy
        Byte4 = 0x04uy
        Byte5 = 0x05uy
        Byte6 = 0x06uy
        Byte7 = 0x07uy
        Byte8 = 0x08uy
        Byte9 = 0x09uy
        ByteA = 0x0Auy
        ByteB = 0x0Buy
        ByteC = 0x0Cuy
        ByteD = 0x0Duy
        ByteE = 0x0Euy
        ByteF = 0x0Fuy
        ByteG = 0x10uy
        ByteH = 0x11uy
        ByteI = 0x12uy
        ByteJ = 0x13uy
    }, hash)

    Assert.Equal<byte>(bytes, hash.ToBytes())

    let e = Assert.Throws<Exception>(fun() -> Sha1Hash.OfBytes Array.empty |> ignore)
    Assert.Equal("Invalid hash array length: 0 ().", e.Message)

[<Fact>]
let ``OfString should work as expected``(): unit =
    let string = "000102030405060708090a0b0c0d0e0f10111213"
    let hash = Sha1Hash.OfHexString string
    Assert.Equal({
        Byte0 = 0x00uy
        Byte1 = 0x01uy
        Byte2 = 0x02uy
        Byte3 = 0x03uy
        Byte4 = 0x04uy
        Byte5 = 0x05uy
        Byte6 = 0x06uy
        Byte7 = 0x07uy
        Byte8 = 0x08uy
        Byte9 = 0x09uy
        ByteA = 0x0Auy
        ByteB = 0x0Buy
        ByteC = 0x0Cuy
        ByteD = 0x0Duy
        ByteE = 0x0Euy
        ByteF = 0x0Fuy
        ByteG = 0x10uy
        ByteH = 0x11uy
        ByteI = 0x12uy
        ByteJ = 0x13uy
    }, hash)

    Assert.Equal(string, hash.ToString())

    let e = Assert.Throws<Exception>(fun() -> Sha1Hash.OfHexString "" |> ignore)
    Assert.Equal("Invalid hash: \"\".", e.Message)
