module Fenrir.Sha1

open System
open System.Buffers.Binary
open System.IO
open Microsoft.FSharp.Core

type HashValue =
    { A: uint32
      B: uint32
      C: uint32
      D: uint32
      E: uint32 }

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
    
let calcChunk (bytes: Span<byte>) (hashValues: HashValue): HashValue =
    let w = Array.zeroCreate<uint32> 80
    for i in 0..15 do
        w[i] <- BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(bytes.Slice(i * 4, 4)))
    for i in 16..79 do
        w[i] <- leftRotate (w[i - 3] ^^^ w[i - 8] ^^^ w[i - 14] ^^^ w[i - 16]) 1
    
    let mutable { A = a; B = b; C = c; D = d; E = e } = hashValues
    
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

        let tmp = ((leftRotate a 5) + f + e + k + w[i])
        e <- d
        d <- c
        c <- leftRotate b 30
        b <- a
        a <- tmp

    {
         A = (hashValues.A + a)
         B = (hashValues.B + b)
         C = (hashValues.C + c)
         D = (hashValues.D + d)
         E = (hashValues.E + e)
    }

let setSizeToSpan (span: Span<byte>) (size: int64) =
    let sizeBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(size * 8L))
    for i in 0..7 do
        span[56 + i] <- sizeBytes[i]

let processTail (hashValue: HashValue) (bytesCount: int64) (tail: Span<byte>): HashValue =
    let significantBytesLeft = int(bytesCount % 64L)
    tail[significantBytesLeft] <- 0b10000000uy
    if significantBytesLeft < 55 then
        let fillWithZerosSlice = tail.Slice(significantBytesLeft + 1, 55 - significantBytesLeft)
        fillWithZerosSlice.Fill(0uy)
        setSizeToSpan tail bytesCount
        calcChunk tail hashValue 
    else    
        let fillWithZerosSlice = tail.Slice(significantBytesLeft + 1)
        fillWithZerosSlice.Fill(0uy)
        let newHashValue = calcChunk tail hashValue
        tail.Fill(0uy)
        setSizeToSpan tail bytesCount
        calcChunk tail newHashValue
        
let calcSHA1Hash (data: Stream): byte array =
    let chunk = Array.zeroCreate<byte> 64
    let chunkSpan = Span<byte>(chunk)
    
    let mutable hashValue = initialValues
    let mutable readCount = data.Read(chunkSpan)
    while (readCount = 64) do
        hashValue <- calcChunk chunkSpan hashValue
        readCount <- data.Read(chunkSpan)
    
    hashValue <- processTail hashValue data.Length chunkSpan 
    let res = Array.zeroCreate<byte> 20
    let hash = [| hashValue.A; hashValue.B; hashValue.C; hashValue.D; hashValue.E |]
               |> Array.map BinaryPrimitives.ReverseEndianness
    Buffer.BlockCopy(hash, 0, res, 0, 20)
    res 