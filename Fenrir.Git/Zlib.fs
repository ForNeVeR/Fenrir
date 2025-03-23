// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

/// Functions to work with Deflate compression.
module Fenrir.Git.Zlib

open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression
open ICSharpCode.SharpZipLib.Zip.Compression.Streams

/// Unpacks a deflate stream.
let UnpackObject(input: Stream, output: Stream): unit =
    use deflate = new InflaterInputStream(input, IsStreamOwner = false)
    deflate.CopyTo output

/// Unpacks a deflate stream and returns the packed (processed, input) data size.
let UnpackObjectAndReturnPackedLength(input: Stream, output: Stream): int64 =
    let inflater = Inflater()
    use deflate = new InflaterInputStream(input, inflater, IsStreamOwner = false)
    deflate.CopyTo output
    inflater.TotalIn

/// Packs the input into a deflate stream.
let PackObject(input: Stream, output: Stream): unit =
    use deflate = new DeflaterOutputStream(output, IsStreamOwner = false)
    input.CopyTo deflate

let internal getDecodedStream (input : Stream) : MemoryStream =
    let decodedInput = new MemoryStream()
    UnpackObject(input, decodedInput)
    decodedInput.Position <- 0L
    decodedInput
