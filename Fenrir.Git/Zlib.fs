// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Zlib

open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression
open ICSharpCode.SharpZipLib.Zip.Compression.Streams

let unpackObject (input: Stream) (output: Stream): unit =
    use deflate = new InflaterInputStream(input, IsStreamOwner = false)
    deflate.CopyTo output

let unpackObjectAndReturnPackedLength (input: Stream) (output: Stream): int64 =
    let inflater = Inflater()
    use deflate = new InflaterInputStream(input, inflater, IsStreamOwner = false)
    deflate.CopyTo output
    inflater.TotalIn

let packObject (input: Stream) (output: Stream): unit =
    use deflate = new DeflaterOutputStream(output, IsStreamOwner = false)
    input.CopyTo deflate

let getDecodedStream (input : Stream) : MemoryStream =
    let decodedInput = new MemoryStream()
    unpackObject input decodedInput
    decodedInput.Position <- 0L
    decodedInput
