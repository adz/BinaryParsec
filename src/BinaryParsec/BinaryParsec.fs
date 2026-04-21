namespace BinaryParsec

open System
open System.Runtime.CompilerServices

/// Tracks the current cursor location within contiguous binary input.
///
/// The core parser runner threads this position through every primitive so
/// errors, byte reads, and bit reads all report against the same state model.
[<Struct; IsReadOnlyAttribute>]
type ParsePosition =
    {
        ByteOffset: int
        BitOffset: int
    }

/// Describes a contiguous region of the original input without copying bytes.
///
/// Higher-level protocol and format parsers return slices to keep the core
/// runner span-based while still exposing payload boundaries to callers.
[<Struct; IsReadOnlyAttribute>]
type ByteSlice =
    {
        Offset: int
        Length: int
    }

/// Carries an offset-aware parse failure from the contiguous-input core.
type ParseError =
    {
        Position: ParsePosition
        Message: string
    }

/// Represents the success or failure of a top-level parse.
type ParseResult<'T> = Result<'T, ParseError>

/// Represents a parser that reads from contiguous binary input and advances a shared cursor.
///
/// The success path carries its value and next position in a struct tuple so
/// repeated span-based reads can stay allocation-free.
type ContiguousParser<'T> = delegate of ReadOnlySpan<byte> * ParsePosition -> ParseResult<struct ('T * ParsePosition)>

/// Captures the byte boundaries of one PNG chunk within the original input.
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

/// Reports the transmitted and computed CRC values for a Modbus RTU frame.
[<Struct; IsReadOnlyAttribute>]
type ModbusRtuCrcResult =
    {
        Expected: uint16
        Actual: uint16
        IsMatch: bool
    }

/// Represents one contiguous Modbus RTU frame over the shared binary core.
[<Struct; IsReadOnlyAttribute>]
type ModbusRtuFrame =
    {
        Address: byte
        FunctionCode: byte
        Payload: ByteSlice
        Crc: ModbusRtuCrcResult
    }

/// Provides the computation-expression surface over the low-level contiguous parser shape.
type ContiguousParserBuilder() =
    member _.Bind(parser: ContiguousParser<'T>, binder: 'T -> ContiguousParser<'U>) : ContiguousParser<'U> =
        ContiguousParser<'U>(fun input position ->
            match parser.Invoke(input, position) with
            | Ok(struct (value, nextPosition)) ->
                let nextParser = binder value
                nextParser.Invoke(input, nextPosition)
            | Error error -> Error error)

    member _.Return(value: 'T) : ContiguousParser<'T> =
        ContiguousParser<'T>(fun _ position -> Ok(struct (value, position)))

    member _.ReturnFrom(parser: ContiguousParser<'T>) : ContiguousParser<'T> =
        parser

    member _.Zero() : ContiguousParser<unit> =
        ContiguousParser<unit>(fun _ position -> Ok(struct ((), position)))

/// Helpers for creating and validating binary cursor positions.
[<RequireQualifiedAccess>]
module ParsePosition =
    /// The start of contiguous input at byte 0, bit 0.
    let origin = { ByteOffset = 0; BitOffset = 0 }

    /// Creates a validated cursor position within contiguous input.
    let create byteOffset bitOffset =
        if byteOffset < 0 then
            invalidArg (nameof byteOffset) "Byte offset must be non-negative."

        if bitOffset < 0 || bitOffset > 7 then
            invalidArg (nameof bitOffset) "Bit offset must be between 0 and 7."

        { ByteOffset = byteOffset; BitOffset = bitOffset }

/// Helpers for constructing zero-copy byte ranges over contiguous input.
[<RequireQualifiedAccess>]
module ByteSlice =
    /// Creates a validated zero-copy slice descriptor.
    let create offset length =
        if offset < 0 then
            invalidArg (nameof offset) "Slice offset must be non-negative."

        if length < 0 then
            invalidArg (nameof length) "Slice length must be non-negative."

        { Offset = offset; Length = length }

    /// Resolves a slice descriptor back to a span over the original input.
    let asSpan (input: ReadOnlySpan<byte>) (slice: ByteSlice) : ReadOnlySpan<byte> =
        input.Slice(slice.Offset, slice.Length)

/// Primitive reads and minimal composition over the contiguous binary parser core.
///
/// This module is the thin layer between the cursor-based runner and
/// higher-level format or protocol parsers.
[<RequireQualifiedAccess>]
module Contiguous =
    let private readFailure position bytesRequested =
        Error
            {
                Position = position
                Message = $"Unexpected end of input while reading {bytesRequested} byte(s)."
            }

    let private requireByteAligned position =
        if position.BitOffset = 0 then
            Ok()
        else
            Error
                {
                    Position = position
                    Message = "Byte-aligned primitive cannot run when the cursor is at a bit offset."
                }

    let private requireBytes bytesRequested (input: ReadOnlySpan<byte>) position =
        let remaining = input.Length - position.ByteOffset

        if remaining >= bytesRequested then
            Ok()
        else
            readFailure position bytesRequested

    let private advanceBytes count position =
        { ByteOffset = position.ByteOffset + count
          BitOffset = 0 }

    let private advanceBits count position =
        let totalBits = position.BitOffset + count

        { ByteOffset = position.ByteOffset + (totalBits / 8)
          BitOffset = totalBits % 8 }

    /// Returns a successful parser result without advancing the cursor.
    let succeed value position : ParseResult<struct ('T * ParsePosition)> =
        Ok(struct (value, position))

    let internal takeAt count (input: ReadOnlySpan<byte>) position =
        match requireByteAligned position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes count input position with
            | Error error -> Error error
            | Ok() ->
                let slice = ByteSlice.create position.ByteOffset count
                succeed slice (advanceBytes count position)

    let internal u32beAt (input: ReadOnlySpan<byte>) position =
        match requireByteAligned position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes 4 input position with
            | Error error -> Error error
            | Ok() ->
                let start = position.ByteOffset
                let value =
                    (uint32 input[start] <<< 24)
                    ||| (uint32 input[start + 1] <<< 16)
                    ||| (uint32 input[start + 2] <<< 8)
                    ||| uint32 input[start + 3]

                succeed value (advanceBytes 4 position)

    /// Runs a parser from the origin and returns only the parsed value.
    let run (parser: ContiguousParser<'T>) (input: ReadOnlySpan<byte>) : ParseResult<'T> =
        match parser.Invoke(input, ParsePosition.origin) with
        | Ok(struct (value, _)) -> Ok value
        | Error error -> Error error

    /// Builds a failure at an explicit cursor position.
    let failAt position message : ParseResult<'T> =
        Error { Position = position; Message = message }

    /// Lifts a value into a parser that consumes no input.
    let result value =
        ContiguousParser<'T>(fun _ position -> succeed value position)

    /// Transforms a parser result without changing how much input is consumed.
    let map mapping (source: ContiguousParser<'T>) =
        ContiguousParser<'U>(fun input position ->
            match source.Invoke(input, position) with
            | Ok(struct (value, nextPosition)) -> succeed (mapping value) nextPosition
            | Error error -> Error error)

    /// Chooses the next parser from the previous parsed value.
    let bind (binder: 'T -> ContiguousParser<'U>) (source: ContiguousParser<'T>) =
        ContiguousParser<'U>(fun input position ->
            match source.Invoke(input, position) with
            | Ok(struct (value, nextPosition)) ->
                let nextParser = binder value
                nextParser.Invoke(input, nextPosition)
            | Error error -> Error error)

    /// Sequences two parsers and returns both parsed values.
    let zip left right =
        bind (fun leftValue -> map (fun rightValue -> leftValue, rightValue) right) left

    /// Runs two parsers and keeps the value produced by the right parser.
    let keepRight left right =
        bind (fun () -> right) left

    /// Runs two parsers and keeps the value produced by the left parser.
    let keepLeft left right =
        bind (fun leftValue -> map (fun () -> leftValue) right) left

    /// Computation-expression builder for contiguous parsers.
    let parse = ContiguousParserBuilder()

    /// Reads one byte and advances to the next byte-aligned position.
    let ``byte`` =
        ContiguousParser<byte>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 1 input position with
                | Error error -> Error error
                | Ok() ->
                    let value = input[position.ByteOffset]
                    succeed value (advanceBytes 1 position))

    /// Reads one byte without advancing the cursor.
    let peekByte =
        ContiguousParser<byte>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 1 input position with
                | Error error -> Error error
                | Ok() -> succeed input[position.ByteOffset] position)

    /// Advances by a byte count without returning any data.
    let skip count =
        if count < 0 then
            invalidArg (nameof count) "Skip count must be non-negative."

        ContiguousParser<unit>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes count input position with
                | Error error -> Error error
                | Ok() -> succeed () (advanceBytes count position))

    /// Returns a zero-copy slice of the next `count` bytes.
    let take count =
        if count < 0 then
            invalidArg (nameof count) "Take count must be non-negative."

        ContiguousParser<ByteSlice>(fun input position -> takeAt count input position)

    /// Reads an unsigned 16-bit integer in big-endian byte order.
    let u16be =
        ContiguousParser<uint16>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 2 input position with
                | Error error -> Error error
                | Ok() ->
                    let start = position.ByteOffset
                    let value =
                        (uint16 input[start] <<< 8)
                        ||| uint16 input[start + 1]

                    succeed value (advanceBytes 2 position))

    /// Reads an unsigned 16-bit integer in little-endian byte order.
    let u16le =
        ContiguousParser<uint16>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 2 input position with
                | Error error -> Error error
                | Ok() ->
                    let start = position.ByteOffset
                    let value =
                        uint16 input[start]
                        ||| (uint16 input[start + 1] <<< 8)

                    succeed value (advanceBytes 2 position))

    /// Reads an unsigned 32-bit integer in big-endian byte order.
    let u32be =
        ContiguousParser<uint32>(fun input position -> u32beAt input position)

    /// Reads one bit in most-significant-bit-first order.
    let bit =
        ContiguousParser<bool>(fun input position ->
            match requireBytes 1 input position with
            | Error error -> Error error
            | Ok() ->
                let current = input[position.ByteOffset]
                let shift = 7 - position.BitOffset
                let value = ((current >>> shift) &&& 1uy) = 1uy
                succeed value (advanceBits 1 position))

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

/// The first Modbus RTU parser slice built on the shared contiguous core.
[<RequireQualifiedAccess>]
module ModbusRtu =
    let private minimumFrameLength = 4

    let private incompleteFrameMessage =
        "Modbus RTU frame must contain address, function code, and CRC."

    let private computeCrc (input: ReadOnlySpan<byte>) (slice: ByteSlice) =
        let mutable crc = 0xFFFFus
        let bytes = ByteSlice.asSpan input slice

        for index = 0 to bytes.Length - 1 do
            crc <- crc ^^^ uint16 bytes[index]

            for _ = 0 to 7 do
                if (crc &&& 0x0001us) = 0x0001us then
                    crc <- (crc >>> 1) ^^^ 0xA001us
                else
                    crc <- crc >>> 1

        crc

    /// Parses one contiguous Modbus RTU frame and reports the transmitted versus computed CRC.
    let frame =
        ContiguousParser<ModbusRtuFrame>(fun input position ->
            if position.BitOffset <> 0 then
                Contiguous.failAt position "Byte-aligned primitive cannot run when the cursor is at a bit offset."
            else
                let remaining = input.Length - position.ByteOffset

                if remaining < minimumFrameLength then
                    Contiguous.failAt position incompleteFrameMessage
                else
                    let payloadLength = remaining - minimumFrameLength
                    let crcOffset = position.ByteOffset + 2 + payloadLength

                    let frameBytes = ByteSlice.create position.ByteOffset (remaining - 2)
                    let payload = ByteSlice.create (position.ByteOffset + 2) payloadLength
                    let expected =
                        uint16 input[crcOffset]
                        ||| (uint16 input[crcOffset + 1] <<< 8)

                    let actual = computeCrc input frameBytes

                    Ok(
                        struct (
                            { Address = input[position.ByteOffset]
                              FunctionCode = input[position.ByteOffset + 1]
                              Payload = payload
                              Crc =
                                { Expected = expected
                                  Actual = actual
                                  IsMatch = expected = actual } },
                            ParsePosition.create input.Length 0
                        )
                    ))
