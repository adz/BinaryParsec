namespace BinaryParsec.Protocols.Png

open System.Runtime.CompilerServices
open BinaryParsec

/// Captures the PNG signature and first chunk as the initial PNG pressure-test slice.
[<Struct; IsReadOnlyAttribute>]
type PngSlice =
    {
        Signature: ByteSlice
        FirstChunk: PngChunkEnvelope
    }
