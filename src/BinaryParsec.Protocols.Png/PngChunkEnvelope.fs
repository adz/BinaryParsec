namespace BinaryParsec.Protocols.Png

open System.Runtime.CompilerServices
open BinaryParsec

/// Captures the byte boundaries of one PNG chunk within the original input.
///
/// The PNG package keeps chunk envelope slices zero-copy so callers can inspect
/// chunk boundaries without forcing payload materialization.
[<Struct; IsReadOnlyAttribute>]
type PngChunkEnvelope =
    {
        Length: uint32
        ChunkType: ByteSlice
        Payload: ByteSlice
        Crc: ByteSlice
    }
