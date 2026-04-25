namespace BinaryParsec.Protocols.Png

open System.Runtime.CompilerServices
open BinaryParsec

/// Captures the PNG signature and chunk envelopes for a whole-file chunk walk.
///
/// The package keeps this zero-copy shape available for callers that need chunk
/// boundaries without immediately materializing a stable owned model.
[<Struct; IsReadOnlyAttribute>]
type PngChunkStream =
    {
        Signature: ByteSlice
        Chunks: PngChunkEnvelope array
    }
