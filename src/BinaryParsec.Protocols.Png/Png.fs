namespace BinaryParsec.Protocols.Png

open System
open System.IO
open BinaryParsec

/// PNG chunk tokenizers and file-level parse entry points over the contiguous core.
[<RequireQualifiedAccess>]
module Png =
    /// Matches the 8-byte PNG file signature and returns its input slice.
    let signature = PngParser.signature

    /// Parses one PNG chunk envelope and returns zero-copy slices for its parts.
    let chunkEnvelope = PngParser.chunkEnvelope

    /// Parses the PNG signature followed by chunk envelopes through the `IEND` terminator.
    let chunkStream = PngParser.chunkStream

    /// Parses the PNG signature followed by the first chunk.
    let initialSlice = PngParser.initialSlice

    /// Parses one whole PNG datastream into a validated owned PNG model.
    let file =
        ContiguousParser<PngFile>(fun input position ->
            match chunkStream.Invoke(input, position) with
            | Error error -> Error error
            | Ok(struct (stream, nextPosition)) ->
                match PngMaterializer.materializeFile input stream nextPosition with
                | Error error -> Error error
                | Ok parsed -> Ok(struct (parsed, nextPosition)))

    /// Parses one whole PNG datastream and returns a result-oriented PNG file model.
    let tryParseFile (input: ReadOnlySpan<byte>) : ParseResult<PngFile> =
        Contiguous.run file input

    /// Parses one whole PNG datastream or raises `InvalidDataException` when the input is invalid.
    let parseFile (input: ReadOnlySpan<byte>) : PngFile =
        match tryParseFile input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn
