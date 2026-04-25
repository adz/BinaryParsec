namespace BinaryParsec.Protocols.Can

open System
open System.IO
open BinaryParsec

/// Classic CAN controller-frame parsers over the contiguous core.
///
/// The current package reads the common controller-buffer layout for base
/// 11-bit frames and keeps later owned-model materialization separate from the
/// packed tokenization step.
[<RequireQualifiedAccess>]
module CanClassic =
    /// Parses one classic controller frame and returns a zero-copy slice.
    let frame = CanClassicParser.frame

    let private ownedFrame =
        ContiguousParser<CanClassicFrame>(fun input position ->
            match frame.Invoke(input, position) with
            | Error error -> Error error
            | Ok(struct (slice, nextPosition)) ->
                match CanClassicMaterializer.materializeFrame input slice nextPosition with
                | Error error -> Error error
                | Ok parsed -> Ok(struct (parsed, nextPosition)))

    /// Parses one classic controller frame and returns a result-oriented owned model.
    let tryParseFrame (input: ReadOnlySpan<byte>) : ParseResult<CanClassicFrame> =
        Contiguous.run ownedFrame input

    /// Parses one classic controller frame or raises `InvalidDataException` when the input is invalid.
    let parseFrame (input: ReadOnlySpan<byte>) : CanClassicFrame =
        match tryParseFrame input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn
