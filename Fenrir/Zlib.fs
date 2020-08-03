module Fenrir.Zlib

open System.IO
open ICSharpCode.SharpZipLib.Zip.Compression.Streams

let unpackObject (input: Stream) (output: Stream): unit =
    use deflate = new InflaterInputStream(input)
    deflate.CopyTo output

let packObject (input: Stream) (output: Stream): unit =
    use deflate = new DeflaterOutputStream(output, IsStreamOwner = false)
    input.CopyTo deflate
let getDecodedStream (input : Stream) : MemoryStream =
    let decodedInput = new MemoryStream()
    unpackObject input decodedInput
    decodedInput.Position <- 0L
    decodedInput
