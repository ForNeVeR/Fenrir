// SPDX-FileCopyrightText: 2017 Marc Stevens <marc@marc-stevens.nl>, Dan Shumow <danshu@microsoft.com>
// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT
module Fenrir.Git.Sha1

open System
open System.Buffers.Binary
open System.IO
open Microsoft.FSharp.Core
open Fenrir.Git.UbcCheck
#nowarn "3391" // implicit conversion from Span to ReadOnlySpan

type private HashValue = {
    mutable A: uint32
    mutable B: uint32
    mutable C: uint32
    mutable D: uint32
    mutable E: uint32
}

let inline private sum (lhs: HashValue) (rhs: HashValue) = {
    A = lhs.A + rhs.A
    B = lhs.B + rhs.B
    C = lhs.C + rhs.C
    D = lhs.D + rhs.D
    E = lhs.E + rhs.E
}

type private CalcHashContext = {
    W: uint32 array
    mutable W2: uint32 array | null
    // store only this states because it defined in https://github.com/cr-marcstevens/sha1collisiondetection/blob/master/lib/ubc_check.h#L39
    State58: HashValue
    State65: HashValue
    mutable HashValue: HashValue
    mutable HashValue1: HashValue
    mutable HashValue2: HashValue
}

[<Literal>]
let private k1 = 0x5A827999u
[<Literal>]
let private k2 = 0x6ED9EBA1u
[<Literal>]
let private k3 = 0x8F1BBCDCu
[<Literal>]
let private k4 = 0xCA62C1D6u

let private initialValue (): HashValue = {
    A = 0x67452301u
    B = 0xEFCDAB89u
    C = 0x98BADCFEu
    D = 0x10325476u
    E = 0xC3D2E1F0u
}

let private emptyValue (): HashValue = {
    A = 0u
    B = 0u
    C = 0u
    D = 0u
    E = 0u
}

let inline private rotateLeft (num: uint32) (shift: int): uint32 =
    ((num <<< shift) ||| (num >>> (32 - shift)))

let inline private rotateRight (num: uint32) (shift: int): uint32 =
    ((num >>> shift) ||| (num <<< (32 - shift)))

let inline private f1 (b: uint32) (c: uint32) (d: uint32) =
    d ^^^ (b &&& (c ^^^ d))

let inline private f2 (b: uint32) (c: uint32) (d: uint32) =
    b ^^^ c ^^^ d

let inline private f3 (b: uint32) (c: uint32) (d: uint32) =
    (b &&& c) + (d &&& (b ^^^ c))

let inline private f4 (b: uint32) (c: uint32) (d: uint32) =
    b ^^^ c ^^^ d

let inline private getCoefs (f: byref<uint32>) (k: byref<uint32>) (step: int) (b: uint32) (c: uint32) (d: uint32) =
    if (0 <= step && step <= 19) then
        f <- f1 b c d
        k <- k1
    else if (20 <= step && step <= 39) then
        f <- f2 b c d
        k <- k2
    else if (40 <= step && step <= 59) then
        f <- f3 b c d
        k <- k3
    else
        f <- f4 b c d
        k <- k4

let inline private shiftToStep (hash: HashValue) (step: int): HashValue =
    match step % 5 with
    | 0 -> hash
    | 1 ->
        { A = hash.E
          B = hash.A
          C = hash.B
          D = hash.C
          E = hash.D }
    | 2 ->
        { A = hash.D
          B = hash.E
          C = hash.A
          D = hash.B
          E = hash.C }
    | 3 ->
        { A = hash.C
          B = hash.D
          C = hash.E
          D = hash.A
          E = hash.B }
    | 4 ->
        { A = hash.B
          B = hash.C
          C = hash.D
          D = hash.E
          E = hash.A }
    | _ -> failwith "Incorrect reminder"

let inline private store (state: HashValue) (a: uint32) (b: uint32) (c: uint32) (d: uint32) (e: uint32) =
    state.A <- a
    state.B <- b
    state.C <- c
    state.D <- d
    state.E <- e

let inline private runForward (fromStep: int) (w: uint32 array) (initialValue: HashValue) (doStoreState: bool) (context: CalcHashContext) : HashValue =
    let mutable { A = a; B = b; C = c; D = d; E = e } = shiftToStep initialValue fromStep
    let mutable f = 0u
    let mutable k = 0u
    let mutable tmp = 0u
    for i = fromStep to 79 do
        if doStoreState then
            if (i = 58) then
                store context.State58 a b c d e
            if (i = 65) then
                store context.State65 a b c d e

        getCoefs &f &k i b c d

        tmp <- e + (rotateLeft a 5) + f + k + w[i]
        e <- d
        d <- c
        c <- rotateLeft b 30
        b <- a
        a <- tmp
    {
        A = a
        B = b
        C = c
        D = d
        E = e
    }

let inline private runBackward (fromStep: int) (w: uint32 array) (initialValue: HashValue): HashValue =
    let mutable { A = a; B = b; C = c; D = d; E = e } = shiftToStep initialValue fromStep
    let mutable f = 0u
    let mutable k = 0u
    let mutable tmp = 0u
    for i = fromStep downto 0 do
        b <- rotateRight b 30
        getCoefs &f &k i b c d

        tmp <- e - ((rotateLeft a 5) + f + k + w[i])
        e <- a
        a <- b
        b <- c
        c <- d
        d <- tmp
    {
        A = e
        B = a
        C = b
        D = c
        E = d
    }

let private calcChunk (bytes: ReadOnlySpan<byte>) (context: CalcHashContext) =
    for i = 0 to 79 do
        context.W[i] <- 0u

    for i = 0 to 15 do
        context.W[i] <- BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(bytes.Slice(i * 4, 4)))
    for i = 16 to 79 do
        context.W[i] <- rotateLeft (context.W[i - 3] ^^^ context.W[i - 8] ^^^ context.W[i - 14] ^^^ context.W[i - 16]) 1

    let hash = runForward 0 context.W context.HashValue true context

    context.HashValue <- sum context.HashValue hash

let private recompress (fromStep: int) (context: CalcHashContext) (initialValue: HashValue): HashValue =
    let w2 = nullArgCheck (nameof context.W2) context.W2
    context.HashValue2 <- runBackward (fromStep - 1) w2 initialValue
    let hash = runForward fromStep w2 initialValue false context
    sum context.HashValue2 hash

let private calcChunkWithCollisionCheck (bytes: ReadOnlySpan<byte>) (context: CalcHashContext): unit =
    context.HashValue1.A <- context.HashValue.A
    context.HashValue1.B <- context.HashValue.B
    context.HashValue1.C <- context.HashValue.C
    context.HashValue1.D <- context.HashValue.D
    context.HashValue1.E <- context.HashValue.E

    calcChunk bytes context
    let dvMask = runUbcCheck context.W
    if (dvMask = 0u) then
        ()

    for i = 0 to 31 do
        if (dvMask &&& (1u <<< SHA1DVs[i].maskb) <> 0u) then
            let dvInfo = SHA1DVs[i]
            let w2 =
                match context.W2 with
                | null ->
                    context.W2 <- Array.zeroCreate<uint32> 80
                    nonNull context.W2
                | w2 -> w2

            for j = 0 to 79 do
                w2[j] <- context.W[j] ^^^ dvInfo.dm[j]

            let tmpHash = recompress dvInfo.testt context (if (dvInfo.testt = 58) then context.State58 else context.State65)
            if (0u = ((tmpHash.A ^^^ context.HashValue.A) ||| (tmpHash.B ^^^ context.HashValue.B) ||| (tmpHash.C ^^^ context.HashValue.C) ||| (tmpHash.D ^^^ context.HashValue.D) ||| (tmpHash.E ^^^ context.HashValue.E))) then
                context.HashValue <- sum context.HashValue (runForward 0 context.W context.HashValue false context)
                context.HashValue <- sum context.HashValue (runForward 0 context.W context.HashValue false context)

let inline private setSizeToSpan (span: Span<byte>) (size: int64) =
    let sizeBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(size * 8L))
    for i = 0 to 7 do
        span[56 + i] <- sizeBytes[i]

let private processTail (context: CalcHashContext) (bytesCount: int64) (tail: Span<byte>) =
    let significantBytesLeft = int(bytesCount % 64L)
    tail[significantBytesLeft] <- 0b10000000uy
    if significantBytesLeft < 55 then
        let fillWithZerosSlice = tail.Slice(significantBytesLeft + 1, 55 - significantBytesLeft)
        fillWithZerosSlice.Fill(0uy)
        setSizeToSpan tail bytesCount
        calcChunkWithCollisionCheck tail context
    else
        let fillWithZerosSlice = tail.Slice(significantBytesLeft + 1)
        fillWithZerosSlice.Fill(0uy)
        calcChunk tail context
        tail.Fill(0uy)
        setSizeToSpan tail bytesCount
        calcChunkWithCollisionCheck tail context

/// <summary>
/// Calculates the hardened SHA-1 hash for the passed stream's content.
/// See <a href="https://github.com/cr-marcstevens/sha1collisiondetection/">the documentation</a> for more details.
/// </summary>
/// <param name="data">Input data stream.</param>
let CalculateHardened (data: Stream): Sha1Hash =
    let mutable context: CalcHashContext = {
        HashValue = initialValue ()
        HashValue1 = emptyValue ()
        HashValue2 = emptyValue ()
        State58 = emptyValue ()
        State65 = emptyValue ()
        W = Array.zeroCreate<uint32> 80
        W2 = null
    }

    let chunk = Array.zeroCreate<byte> 64
    let chunkSpan = Span<byte>(chunk)
    let mutable l = 0;
    let mutable readCount = data.Read(chunkSpan)
    while (readCount = 64) do
        l <- readCount + l
        calcChunkWithCollisionCheck chunkSpan context
        readCount <- data.Read(chunkSpan)

    processTail context data.Length chunkSpan
    let res = Array.zeroCreate<byte> 20
    let hash = [| context.HashValue.A; context.HashValue.B; context.HashValue.C; context.HashValue.D; context.HashValue.E |]
               |> Array.map BinaryPrimitives.ReverseEndianness
    Buffer.BlockCopy(hash, 0, res, 0, 20)
    res |> Sha1Hash.OfBytes
