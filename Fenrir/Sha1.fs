module Fenrir.Sha1

open System
open System.Buffers.Binary
open System.IO
open Microsoft.FSharp.Core
#nowarn "3391" // implicit conversion from Span to ReadOnlySpan

type HashValue = {
    mutable A: uint32
    mutable B: uint32
    mutable C: uint32
    mutable D: uint32
    mutable E: uint32
}

type CalcHashContext = {
    W: uint32 array
    mutable HashValue: HashValue
}

[<Literal>]
let k1 = 0x5A827999u
[<Literal>]
let k2 = 0x6ED9EBA1u
[<Literal>]
let k3 = 0x8F1BBCDCu
[<Literal>]
let k4 = 0xCA62C1D6u

let initialValues: HashValue =
    { A = 0x67452301u
      B = 0xEFCDAB89u
      C = 0x98BADCFEu
      D = 0x10325476u
      E = 0xC3D2E1F0u }

let leftRotate (num: uint32) (size: int): uint32 =
    ((num <<< size) ||| (num >>> (32 - size)))
    
let calcChunk (bytes: ReadOnlySpan<byte>) (context: CalcHashContext) =
    for i in 0..79 do
        context.W[i] <- 0u
        
    for i in 0..15 do
        context.W[i] <- BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(bytes.Slice(i * 4, 4)))
    for i in 16..79 do
        context.W[i] <- leftRotate (context.W[i - 3] ^^^ context.W[i - 8] ^^^ context.W[i - 14] ^^^ context.W[i - 16]) 1
    
    let mutable { A = a; B = b; C = c; D = d; E = e } = context.HashValue
    
    let mutable f = 0u
    let mutable k = 0u
    for i in 0..79 do
        if (0 <= i && i <= 19) then
            f <- d ^^^ (b &&& (c ^^^ d))
            k <- k1
        else if (20 <= i && i <= 39) then
            f <- b ^^^ c ^^^ d
            k <- k2
        else if (40 <= i && i<= 59) then
            f <- (b &&& c) ||| (b &&& d) ||| (c &&& d)
            k <- k3
        else
            f <- b ^^^ c ^^^ d
            k <- k4

        let tmp = ((leftRotate a 5) + f + e + k + context.W[i])
        e <- d
        d <- c
        c <- leftRotate b 30
        b <- a
        a <- tmp

    context.HashValue.A <- context.HashValue.A + a
    context.HashValue.B <- context.HashValue.B + b
    context.HashValue.C <- context.HashValue.C + c
    context.HashValue.D <- context.HashValue.D + d
    context.HashValue.E <- context.HashValue.E + e

let setSizeToSpan (span: Span<byte>) (size: int64) =
    let sizeBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(size * 8L))
    for i in 0..7 do
        span[56 + i] <- sizeBytes[i]

let processTail (context: CalcHashContext) (bytesCount: int64) (tail: Span<byte>) =
    let significantBytesLeft = int(bytesCount % 64L)
    tail[significantBytesLeft] <- 0b10000000uy
    if significantBytesLeft < 55 then
        let fillWithZerosSlice = tail.Slice(significantBytesLeft + 1, 55 - significantBytesLeft)
        fillWithZerosSlice.Fill(0uy)
        setSizeToSpan tail bytesCount
        calcChunk tail context 
    else    
        let fillWithZerosSlice = tail.Slice(significantBytesLeft + 1)
        fillWithZerosSlice.Fill(0uy)
        calcChunk tail context
        tail.Fill(0uy)
        setSizeToSpan tail bytesCount
        calcChunk tail context
        
let calcSHA1Hash (data: Stream): byte array =
    let mutable context: CalcHashContext = {
        HashValue = initialValues;
        W = Array.zeroCreate<uint32> 80
    }
    
    let chunk = Array.zeroCreate<byte> 64
    let chunkSpan = Span<byte>(chunk)
    
    let mutable readCount = data.Read(chunkSpan)
    while (readCount = 64) do
        calcChunk chunkSpan context
        readCount <- data.Read(chunkSpan)
    
    processTail context data.Length chunkSpan 
    let res = Array.zeroCreate<byte> 20
    let hash = [| context.HashValue.A; context.HashValue.B; context.HashValue.C; context.HashValue.D; context.HashValue.E |]
               |> Array.map BinaryPrimitives.ReverseEndianness
    Buffer.BlockCopy(hash, 0, res, 0, 20)
    res 