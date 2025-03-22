namespace Fenrir.Git

open System
open System.Runtime.InteropServices

/// Structure to keep SHA-1 hash.
/// Guarantees the stored hash is valid, will normalize it to lowercase when converted to a string.
[<Struct>]
type Sha1Hash =
    {
        Byte0: byte
        Byte1: byte
        Byte2: byte
        Byte3: byte
        Byte4: byte
        Byte5: byte
        Byte6: byte
        Byte7: byte
        Byte8: byte
        Byte9: byte
        ByteA: byte
        ByteB: byte
        ByteC: byte
        ByteD: byte
        ByteE: byte
        ByteF: byte
        ByteG: byte
        ByteH: byte
        ByteI: byte
        ByteJ: byte
    }

    /// <summary>Size of the SHA-1 hash in bytes.</summary>
    /// <remarks>Note that this is not the length of the string representation.</remarks>
    static member SizeInBytes = 20

    /// A hash object filled with zeros.
    static member Zero = Sha1Hash.OfBytes <| Array.zeroCreate Sha1Hash.SizeInBytes

    /// Converts a byte array to a SHA-1 hash object. Will verify the array length.
    static member OfBytes(bytes: byte[]): Sha1Hash =
        if bytes.Length <> Sha1Hash.SizeInBytes then
            failwithf $"Invalid hash array length: {bytes.Length} ({Convert.ToHexString bytes})."
        {
            Byte0 = bytes[0]
            Byte1 = bytes[1]
            Byte2 = bytes[2]
            Byte3 = bytes[3]
            Byte4 = bytes[4]
            Byte5 = bytes[5]
            Byte6 = bytes[6]
            Byte7 = bytes[7]
            Byte8 = bytes[8]
            Byte9 = bytes[9]
            ByteA = bytes[10]
            ByteB = bytes[11]
            ByteC = bytes[12]
            ByteD = bytes[13]
            ByteE = bytes[14]
            ByteF = bytes[15]
            ByteG = bytes[16]
            ByteH = bytes[17]
            ByteI = bytes[18]
            ByteJ = bytes[19]
        }

    /// Converts a hexadecimal string representation (possibly in a mixed case) into a hash object. Will verify the
    /// input data.
    static member OfHexString(data: string): Sha1Hash =
        if data.Length <> Sha1Hash.SizeInBytes * 2 then failwithf $"Invalid hash: \"{data}\"."
        data |> Convert.FromHexString |> Sha1Hash.OfBytes

    /// <summary>Converts the hash object to a byte array of exactly <see cref="Sha1Hash.SizeInBytes"/> bytes.</summary>
    member this.ToBytes(): byte[] =
        let span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(&this, 1))
        span.ToArray()

    /// Converts the hash object to a hexadecimal lowercase string representation.
    override this.ToString() =
        let span = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(&this, 1))
        Convert.ToHexStringLower span
