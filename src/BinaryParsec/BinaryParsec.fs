namespace BinaryParsec

open System
open System.Runtime.CompilerServices

[<Struct; IsReadOnlyAttribute>]
type ParsePosition =
    {
        ByteOffset: int
        BitOffset: int
    }

[<Struct; IsReadOnlyAttribute>]
type ByteSlice =
    {
        Offset: int
        Length: int
    }

type ParseError =
    {
        Position: ParsePosition
        Message: string
    }

type ParseResult<'T> = Result<'T, ParseError>

type ContiguousParser<'T> = delegate of ReadOnlySpan<byte> * ParsePosition -> ParseResult<'T * ParsePosition>

[<RequireQualifiedAccess>]
module ParsePosition =
    let origin = { ByteOffset = 0; BitOffset = 0 }

    let create byteOffset bitOffset =
        if byteOffset < 0 then
            invalidArg (nameof byteOffset) "Byte offset must be non-negative."

        if bitOffset < 0 || bitOffset > 7 then
            invalidArg (nameof bitOffset) "Bit offset must be between 0 and 7."

        { ByteOffset = byteOffset; BitOffset = bitOffset }

[<RequireQualifiedAccess>]
module ByteSlice =
    let create offset length =
        if offset < 0 then
            invalidArg (nameof offset) "Slice offset must be non-negative."

        if length < 0 then
            invalidArg (nameof length) "Slice length must be non-negative."

        { Offset = offset; Length = length }

    let asSpan (input: ReadOnlySpan<byte>) (slice: ByteSlice) : ReadOnlySpan<byte> =
        input.Slice(slice.Offset, slice.Length)

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

    let run (parser: ContiguousParser<'T>) (input: ReadOnlySpan<byte>) : ParseResult<'T> =
        parser.Invoke(input, ParsePosition.origin)
        |> Result.map fst

    let failAt position message : ParseResult<'T> =
        Error { Position = position; Message = message }

    let succeed value position : ParseResult<'T * ParsePosition> =
        Ok(value, position)

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

    let peekByte =
        ContiguousParser<byte>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 1 input position with
                | Error error -> Error error
                | Ok() -> succeed input[position.ByteOffset] position)

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

    let take count =
        if count < 0 then
            invalidArg (nameof count) "Take count must be non-negative."

        ContiguousParser<ByteSlice>(fun input position ->
            match requireByteAligned position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes count input position with
                | Error error -> Error error
                | Ok() ->
                    let slice = ByteSlice.create position.ByteOffset count
                    succeed slice (advanceBytes count position))

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

    let bit =
        ContiguousParser<bool>(fun input position ->
            match requireBytes 1 input position with
            | Error error -> Error error
            | Ok() ->
                let current = input[position.ByteOffset]
                let shift = 7 - position.BitOffset
                let value = ((current >>> shift) &&& 1uy) = 1uy
                succeed value (advanceBits 1 position))
