namespace BinaryParsec.Protocols.Png

open System
open BinaryParsec
open BinaryParsec.Syntax

[<RequireQualifiedAccess>]
module internal PngParser =
    let internal signatureBytes =
        [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

    let internal ihdrChunkTypeBytes =
        [| 0x49uy; 0x48uy; 0x44uy; 0x52uy |]

    let internal plteChunkTypeBytes =
        [| 0x50uy; 0x4Cuy; 0x54uy; 0x45uy |]

    let internal idatChunkTypeBytes =
        [| 0x49uy; 0x44uy; 0x41uy; 0x54uy |]

    let internal iendChunkTypeBytes =
        [| 0x49uy; 0x45uy; 0x4Euy; 0x44uy |]

    let internal invalidSignatureMessage =
        "Input does not start with the PNG file signature."

    let internal invalidLengthMessage =
        "PNG chunk length exceeds supported contiguous input size."

    let private maxSupportedChunkLength = uint32 Int32.MaxValue - 4u

    let internal chunkTypeMatches (expectedBytes: byte array) (input: ReadOnlySpan<byte>) (chunkType: ByteSlice) =
        let actual = ByteSlice.asSpan input chunkType
        let mutable matches = actual.Length = expectedBytes.Length
        let mutable index = 0

        while matches && index < actual.Length do
            if actual[index] <> expectedBytes[index] then
                matches <- false

            index <- index + 1

        matches

    let signature = expectBytes signatureBytes invalidSignatureMessage

    let chunkEnvelope =
        ContiguousParser<PngChunkEnvelope>(fun input position ->
            match Contiguous.position.Invoke(input, position) with
            | Error error -> Error error
            | Ok struct (pos, _) ->
                match Contiguous.u32be.Invoke(input, position) with
                | Error error -> Error error
                | Ok struct (length, afterLength) ->
                    match Contiguous.takeAt 4 input afterLength with
                    | Error error -> Error error
                    | Ok struct (chunkType, afterChunkType) ->
                        if length > maxSupportedChunkLength then
                            failAt pos invalidLengthMessage
                        else
                            match Contiguous.takeAt (int length) input afterChunkType with
                            | Error error -> Error error
                            | Ok struct (payload, afterPayload) ->
                                match Contiguous.takeAt 4 input afterPayload with
                                | Error error -> Error error
                                | Ok struct (crc, nextPosition) ->
                                    Ok
                                        struct (
                                            { Length = length
                                              ChunkType = chunkType
                                              Payload = payload
                                              Crc = crc },
                                            nextPosition
                                        ))

    let chunkStream =
        ContiguousParser<PngChunkStream>(fun input position ->
            match signature.Invoke(input, position) with
            | Error error -> Error error
            | Ok struct (sigSlice, afterSignature) ->
                let mutable currentPosition = afterSignature
                let mutable finished = false
                let mutable failure = None
                let chunks = ResizeArray<PngChunkEnvelope>()

                while not finished && failure.IsNone do
                    match chunkEnvelope.Invoke(input, currentPosition) with
                    | Error error ->
                        failure <- Some error
                    | Ok struct (envelope, nextPosition) ->
                        chunks.Add envelope
                        currentPosition <- nextPosition
                        finished <- chunkTypeMatches iendChunkTypeBytes input envelope.ChunkType

                match failure with
                | Some error -> Error error
                | None ->
                    Ok
                        struct (
                            { Signature = sigSlice
                              Chunks = chunks.ToArray() },
                            currentPosition
                        ))

    let initialSlice =
        map2
            (fun parsedSignature firstChunk ->
                { Signature = parsedSignature
                  FirstChunk = firstChunk })
            signature
            chunkEnvelope
