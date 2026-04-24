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

/// Captures the PNG signature and chunk envelopes for a small whole-file chunk walk.
///
/// This stays intentionally small: it exists to pressure repeated bounded reads and
/// chunk iteration in the core before the package grows into fuller PNG support.
[<Struct; IsReadOnlyAttribute>]
type PngChunkStream =
    {
        Signature: ByteSlice
        Chunks: PngChunkEnvelope array
    }

/// The first PNG-focused parsers that pressure the contiguous core with a real format.
[<RequireQualifiedAccess>]
module Png =
    let private maxSupportedChunkLength = uint32 Int32.MaxValue - 4u

    let private signatureBytes =
        [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

    let private iendChunkTypeBytes =
        [| 0x49uy; 0x45uy; 0x4Euy; 0x44uy |]

    let private invalidSignatureMessage =
        "Input does not start with the PNG file signature."

    let private invalidLengthMessage =
        "PNG chunk length exceeds supported contiguous input size."

    let private chunkTypeMatches (expectedBytes: byte array) (input: ReadOnlySpan<byte>) (chunkType: ByteSlice) =
        let actual = ByteSlice.asSpan input chunkType
        let mutable matches = actual.Length = expectedBytes.Length
        let mutable index = 0

        while matches && index < actual.Length do
            if actual[index] <> expectedBytes[index] then
                matches <- false

            index <- index + 1

        matches

    /// Matches the 8-byte PNG file signature and returns its input slice.
    let signature = Contiguous.expectBytes signatureBytes invalidSignatureMessage

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

    /// Parses the PNG signature followed by chunk envelopes through the `IEND` terminator.
    let chunkStream =
        ContiguousParser<PngChunkStream>(fun input position ->
            match signature.Invoke(input, position) with
            | Error error -> Error error
            | Ok(struct (parsedSignature, afterSignature)) ->
                let chunks = ResizeArray<PngChunkEnvelope>()
                let mutable current = afterSignature
                let mutable finished = false
                let mutable failure = ValueNone

                while not finished && ValueOption.isNone failure do
                    match chunkEnvelope.Invoke(input, current) with
                    | Error error ->
                        failure <- ValueSome error
                    | Ok(struct (chunk, nextPosition)) ->
                        chunks.Add(chunk)
                        current <- nextPosition
                        let chunkType = chunk.ChunkType
                        finished <- chunkTypeMatches iendChunkTypeBytes input chunkType

                match failure with
                | ValueSome error -> Error error
                | ValueNone ->
                    Ok(
                        struct (
                            { Signature = parsedSignature
                              Chunks = chunks.ToArray() },
                            current
                        )
                    ))

    /// Parses the PNG signature followed by the first chunk.
    let initialSlice =
        Contiguous.parse {
            let! parsedSignature = signature
            and! firstChunk = chunkEnvelope

            return
                { Signature = parsedSignature
                  FirstChunk = firstChunk }
        }
