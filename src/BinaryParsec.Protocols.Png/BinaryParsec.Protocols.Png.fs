namespace BinaryParsec.Protocols.Png

open System
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

/// Captures the PNG signature and first chunk as the initial PNG pressure-test slice.
[<Struct; IsReadOnlyAttribute>]
type PngSlice =
    {
        Signature: ByteSlice
        FirstChunk: PngChunkEnvelope
    }

/// The first PNG-focused parsers that pressure the contiguous core with a real format.
[<RequireQualifiedAccess>]
module Png =
    let private maxSupportedChunkLength = uint32 Int32.MaxValue - 4u

    let private signatureBytes =
        [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

    let private invalidSignatureMessage =
        "Input does not start with the PNG file signature."

    let private invalidLengthMessage =
        "PNG chunk length exceeds supported contiguous input size."

    /// Matches the 8-byte PNG file signature and returns its input slice.
    let signature =
        ContiguousParser<ByteSlice>(fun input position ->
            match Contiguous.takeAt signatureBytes.Length input position with
            | Error error -> Error error
            | Ok(struct (slice, nextPosition)) ->
                let actual = ByteSlice.asSpan input slice
                let mutable matches = true
                let mutable index = 0

                while matches && index < signatureBytes.Length do
                    if actual[index] <> signatureBytes[index] then
                        matches <- false

                    index <- index + 1

                if matches then
                    Ok(struct (slice, nextPosition))
                else
                    Contiguous.failAt position invalidSignatureMessage)

    /// Parses one PNG chunk envelope and returns zero-copy slices for its parts.
    let chunkEnvelope =
        ContiguousParser<PngChunkEnvelope>(fun input position ->
            match Contiguous.u32beAt input position with
            | Error error -> Error error
            | Ok(struct (length, afterLength)) ->
                match Contiguous.takeAt 4 input afterLength with
                | Error error -> Error error
                | Ok(struct (chunkType, afterChunkType)) ->
                    if length > maxSupportedChunkLength then
                        Contiguous.failAt position invalidLengthMessage
                    else
                        match Contiguous.takeAt (int length) input afterChunkType with
                        | Error error -> Error error
                        | Ok(struct (payload, afterPayload)) ->
                            match Contiguous.takeAt 4 input afterPayload with
                            | Error error -> Error error
                            | Ok(struct (crc, nextPosition)) ->
                                Ok(
                                    struct (
                                        { Length = length
                                          ChunkType = chunkType
                                          Payload = payload
                                          Crc = crc },
                                        nextPosition
                                    )
                                ))

    /// Parses the PNG signature followed by the first chunk.
    let initialSlice =
        ContiguousParser<PngSlice>(fun input position ->
            match signature.Invoke(input, position) with
            | Error error -> Error error
            | Ok(struct (parsedSignature, afterSignature)) ->
                match chunkEnvelope.Invoke(input, afterSignature) with
                | Error error -> Error error
                | Ok(struct (firstChunk, nextPosition)) ->
                    Ok(
                        struct (
                            { Signature = parsedSignature
                              FirstChunk = firstChunk },
                            nextPosition
                        )
                    ))
