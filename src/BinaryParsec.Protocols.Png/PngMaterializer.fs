namespace BinaryParsec.Protocols.Png

open System
open BinaryParsec

[<RequireQualifiedAccess>]
module internal PngMaterializer =
    let private invalidIhdrMessage =
        "PNG must begin with an IHDR chunk."

    let private duplicateIhdrMessage =
        "PNG must contain exactly one IHDR chunk."

    let private missingIdatMessage =
        "PNG must contain at least one IDAT chunk."

    let private nonConsecutiveIdatMessage =
        "PNG IDAT chunks must be consecutive."

    let private invalidIendLengthMessage =
        "PNG IEND chunk must have an empty payload."

    let private invalidIhdrLengthMessage =
        "PNG IHDR chunk must contain exactly 13 data bytes."

    let private invalidDimensionMessage =
        "PNG IHDR width and height must be non-zero."

    let private invalidDimensionRangeMessage =
        "PNG IHDR width and height must not exceed 2^31-1."

    let private invalidColorTypeMessage value =
        $"PNG IHDR color type byte 0x{value:X2} is not supported by the PNG specification."

    let private invalidCompressionMethodMessage value =
        $"PNG IHDR compression method byte 0x{value:X2} is invalid. PNG currently defines only method 0."

    let private invalidFilterMethodMessage value =
        $"PNG IHDR filter method byte 0x{value:X2} is invalid. PNG currently defines only method 0."

    let private invalidInterlaceMethodMessage value =
        $"PNG IHDR interlace method byte 0x{value:X2} is invalid. PNG currently defines only methods 0 and 1."

    let private invalidBitDepthMessage colorType bitDepth =
        $"PNG IHDR bit depth {bitDepth} is invalid for color type {int colorType}."

    let private missingPaletteMessage =
        "PNG indexed-color images must contain a PLTE chunk before IDAT."

    let private forbiddenPaletteMessage =
        "PNG greyscale images must not contain a PLTE chunk."

    let private plteOrderingMessage =
        "PNG PLTE chunk must appear before the first IDAT chunk."

    let private duplicatePlteMessage =
        "PNG must not contain more than one PLTE chunk."

    let private trailingBytesMessage =
        "PNG datastream must end immediately after the IEND chunk."

    let private crcMismatchMessage chunkType expected actual =
        $"PNG chunk CRC mismatch for {chunkType}. Expected 0x{expected:X8}, computed 0x{actual:X8}."

    let private bytesToChunkType (input: ReadOnlySpan<byte>) (chunkType: ByteSlice) =
        let bytes = ByteSlice.asSpan input chunkType
        String.Create(
            bytes.Length,
            bytes,
            fun destination source ->
                for index = 0 to source.Length - 1 do
                    destination[index] <- char source[index]
        )

    let private readU32 (input: ReadOnlySpan<byte>) offset =
        let position = ParsePosition.create offset 0

        match Contiguous.u32beAt input position with
        | Ok(struct (value, _)) -> value
        | Error error -> raise (InvalidOperationException(error.Message))

    let private computeCrc32 (input: ReadOnlySpan<byte>) (chunk: PngChunkEnvelope) =
        let mutable crc = 0xFFFFFFFFu
        let chunkType = ByteSlice.asSpan input chunk.ChunkType
        let payload = ByteSlice.asSpan input chunk.Payload

        let update value =
            crc <- crc ^^^ uint32 value

            for _ = 0 to 7 do
                if (crc &&& 1u) = 1u then
                    crc <- (crc >>> 1) ^^^ 0xEDB88320u
                else
                    crc <- crc >>> 1

        for index = 0 to chunkType.Length - 1 do
            update chunkType[index]

        for index = 0 to payload.Length - 1 do
            update payload[index]

        ~~~crc

    let private validateChunkCrc (input: ReadOnlySpan<byte>) (chunk: PngChunkEnvelope) =
        let expected = readU32 input chunk.Crc.Offset
        let actual = computeCrc32 input chunk

        if expected = actual then
            Ok expected
        else
            let chunkType = bytesToChunkType input chunk.ChunkType

            Contiguous.failAt
                (ParsePosition.create chunk.Crc.Offset 0)
                (crcMismatchMessage chunkType expected actual)

    let private parseColorType colorTypeByte position =
        match int colorTypeByte with
        | 0 -> Ok PngColorType.Greyscale
        | 2 -> Ok PngColorType.Truecolor
        | 3 -> Ok PngColorType.IndexedColor
        | 4 -> Ok PngColorType.GreyscaleWithAlpha
        | 6 -> Ok PngColorType.TruecolorWithAlpha
        | _ -> Contiguous.failAt position (invalidColorTypeMessage colorTypeByte)

    let private parseInterlaceMethod interlaceByte position =
        match int interlaceByte with
        | 0 -> Ok PngInterlaceMethod.None
        | 1 -> Ok PngInterlaceMethod.Adam7
        | _ -> Contiguous.failAt position (invalidInterlaceMethodMessage interlaceByte)

    let private validateBitDepth colorType bitDepth position =
        let isValid =
            match colorType with
            | PngColorType.Greyscale -> bitDepth = 1uy || bitDepth = 2uy || bitDepth = 4uy || bitDepth = 8uy || bitDepth = 16uy
            | PngColorType.Truecolor -> bitDepth = 8uy || bitDepth = 16uy
            | PngColorType.IndexedColor -> bitDepth = 1uy || bitDepth = 2uy || bitDepth = 4uy || bitDepth = 8uy
            | PngColorType.GreyscaleWithAlpha -> bitDepth = 8uy || bitDepth = 16uy
            | PngColorType.TruecolorWithAlpha -> bitDepth = 8uy || bitDepth = 16uy
            | _ -> false

        if isValid then
            Ok()
        else
            Contiguous.failAt position (invalidBitDepthMessage colorType bitDepth)

    let private parseHeader (input: ReadOnlySpan<byte>) (chunk: PngChunkEnvelope) =
        if chunk.Length <> 13u then
            Contiguous.failAt (ParsePosition.create chunk.Payload.Offset 0) invalidIhdrLengthMessage
        else
            let payload = ByteSlice.asSpan input chunk.Payload
            let width = readU32 payload 0
            let height = readU32 payload 4

            if width = 0u || height = 0u then
                Contiguous.failAt (ParsePosition.create chunk.Payload.Offset 0) invalidDimensionMessage
            elif width > uint32 Int32.MaxValue || height > uint32 Int32.MaxValue then
                Contiguous.failAt (ParsePosition.create chunk.Payload.Offset 0) invalidDimensionRangeMessage
            else
                let bitDepth = payload[8]
                let colorTypePosition = ParsePosition.create (chunk.Payload.Offset + 9) 0
                let compressionPosition = ParsePosition.create (chunk.Payload.Offset + 10) 0
                let filterPosition = ParsePosition.create (chunk.Payload.Offset + 11) 0
                let interlacePosition = ParsePosition.create (chunk.Payload.Offset + 12) 0

                match parseColorType payload[9] colorTypePosition with
                | Error error -> Error error
                | Ok colorType ->
                    match validateBitDepth colorType bitDepth (ParsePosition.create (chunk.Payload.Offset + 8) 0) with
                    | Error error -> Error error
                    | Ok() ->
                        if payload[10] <> 0uy then
                            Contiguous.failAt compressionPosition (invalidCompressionMethodMessage payload[10])
                        elif payload[11] <> 0uy then
                            Contiguous.failAt filterPosition (invalidFilterMethodMessage payload[11])
                        else
                            match parseInterlaceMethod payload[12] interlacePosition with
                            | Error error -> Error error
                            | Ok interlaceMethod ->
                                Ok
                                    { Width = width
                                      Height = height
                                      BitDepth = bitDepth
                                      ColorType = colorType
                                      CompressionMethod = payload[10]
                                      FilterMethod = payload[11]
                                      InterlaceMethod = interlaceMethod }

    let private validateSequence (input: ReadOnlySpan<byte>) (stream: PngChunkStream) =
        if stream.Chunks.Length = 0 then
            Contiguous.failAt (ParsePosition.create 8 0) invalidIhdrMessage
        elif not (PngParser.chunkTypeMatches PngParser.ihdrChunkTypeBytes input stream.Chunks[0].ChunkType) then
            Contiguous.failAt (ParsePosition.create stream.Chunks[0].ChunkType.Offset 0) invalidIhdrMessage
        else
            let mutable header: PngImageHeader voption = ValueNone
            let mutable seenPlte = false
            let mutable seenIdat = false
            let mutable leftIdatRun = false
            let mutable index = 0
            let mutable failure = ValueNone

            while index < stream.Chunks.Length && ValueOption.isNone failure do
                let chunk = stream.Chunks[index]
                let chunkTypePosition = ParsePosition.create chunk.ChunkType.Offset 0
                let isIhdr = PngParser.chunkTypeMatches PngParser.ihdrChunkTypeBytes input chunk.ChunkType
                let isPlte = PngParser.chunkTypeMatches PngParser.plteChunkTypeBytes input chunk.ChunkType
                let isIdat = PngParser.chunkTypeMatches PngParser.idatChunkTypeBytes input chunk.ChunkType
                let isIend = PngParser.chunkTypeMatches PngParser.iendChunkTypeBytes input chunk.ChunkType

                match validateChunkCrc input chunk with
                | Error error ->
                    failure <- ValueSome error
                | Ok _ ->
                    if isIhdr then
                        if index <> 0 then
                            failure <- ValueSome { Position = chunkTypePosition; Message = duplicateIhdrMessage }
                        else
                            match parseHeader input chunk with
                            | Error error ->
                                failure <- ValueSome error
                            | Ok parsedHeader ->
                                header <- ValueSome parsedHeader
                    elif isPlte then
                        if seenPlte then
                            failure <- ValueSome { Position = chunkTypePosition; Message = duplicatePlteMessage }
                        elif seenIdat then
                            failure <- ValueSome { Position = chunkTypePosition; Message = plteOrderingMessage }
                        else
                            seenPlte <- true
                    elif isIdat then
                        if leftIdatRun then
                            failure <- ValueSome { Position = chunkTypePosition; Message = nonConsecutiveIdatMessage }
                        else
                            seenIdat <- true
                    elif seenIdat then
                        leftIdatRun <- true

                    if isIend && chunk.Length <> 0u && ValueOption.isNone failure then
                        failure <- ValueSome { Position = ParsePosition.create chunk.Payload.Offset 0; Message = invalidIendLengthMessage }

                index <- index + 1

            match failure with
            | ValueSome error -> Error error
            | ValueNone ->
                if not seenIdat then
                    Contiguous.failAt (ParsePosition.create 8 0) missingIdatMessage
                else
                    match header with
                    | ValueNone ->
                        Contiguous.failAt (ParsePosition.create 8 0) invalidIhdrMessage
                    | ValueSome parsedHeader ->
                        if parsedHeader.ColorType = PngColorType.IndexedColor && not seenPlte then
                            Contiguous.failAt (ParsePosition.create 8 0) missingPaletteMessage
                        elif
                            (parsedHeader.ColorType = PngColorType.Greyscale
                             || parsedHeader.ColorType = PngColorType.GreyscaleWithAlpha)
                            && seenPlte
                        then
                            Contiguous.failAt (ParsePosition.create 8 0) forbiddenPaletteMessage
                        else
                            Ok parsedHeader

    let materializeFile (input: ReadOnlySpan<byte>) (stream: PngChunkStream) endPosition =
        if endPosition.ByteOffset <> input.Length || endPosition.BitOffset <> 0 then
            Contiguous.failAt endPosition trailingBytesMessage
        else
            match validateSequence input stream with
            | Error error -> Error error
            | Ok header ->
                let chunks = Array.zeroCreate<PngChunk> stream.Chunks.Length

                for index = 0 to stream.Chunks.Length - 1 do
                    let chunk = stream.Chunks[index]
                    let chunkType = bytesToChunkType input chunk.ChunkType
                    let payload = ByteSlice.asSpan input chunk.Payload
                    let data = payload.ToArray()
                    let crc = readU32 input chunk.Crc.Offset

                    chunks[index] <-
                        { ChunkType = chunkType
                          Data = data
                          Crc = crc }

                Ok
                    { Header = header
                      Chunks = chunks }
