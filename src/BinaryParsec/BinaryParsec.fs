namespace BinaryParsec

open System

[<Struct>]
type ParsePosition =
    {
        ByteOffset: int
        BitOffset: int
    }

[<Struct>]
type ParseError =
    {
        Position: ParsePosition
        Message: string
    }

type ParseResult<'T> = Result<'T, ParseError>

type SpanParser<'T> = delegate of ReadOnlySpan<byte> -> ParseResult<'T>

module Span =
    let run (parser: SpanParser<'T>) (input: ReadOnlySpan<byte>) =
        parser.Invoke input

    let failAt position message : ParseResult<'T> =
        Error { Position = position; Message = message }

    let succeed value : ParseResult<'T> =
        Ok value

