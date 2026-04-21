namespace BinaryParsec

open System
open System.Runtime.CompilerServices

[<Struct; IsReadOnlyAttribute>]
type ParsePosition =
    {
        ByteOffset: int
        BitOffset: int
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
module Contiguous =
    let run (parser: ContiguousParser<'T>) (input: ReadOnlySpan<byte>) : ParseResult<'T> =
        parser.Invoke(input, ParsePosition.origin)
        |> Result.map fst

    let failAt position message : ParseResult<'T> =
        Error { Position = position; Message = message }

    let succeed value position : ParseResult<'T * ParsePosition> =
        Ok(value, position)
