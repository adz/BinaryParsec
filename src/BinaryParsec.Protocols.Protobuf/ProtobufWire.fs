namespace BinaryParsec.Protocols.Protobuf

open System
open System.IO
open BinaryParsec

/// Protocol Buffers wire-field tokenizers and field-stream parse entry points over the contiguous core.
///
/// The current package intentionally stays at the wire-format layer. It parses
/// field tags plus supported payload encodings and leaves schema-specific
/// message interpretation to higher-level code.
[<RequireQualifiedAccess>]
module ProtobufWire =
    /// Parses one Protocol Buffers wire field into a zero-copy field slice.
    let field = ProtobufWireParser.field

    let private ownedField =
        ContiguousParser<ProtobufField>(fun input position ->
            match field.Invoke(input, position) with
            | Error error -> Error error
            | Ok(struct (parsedField: ProtobufFieldSlice, nextPosition)) ->
                if nextPosition.ByteOffset <> input.Length then
                    Contiguous.failAt nextPosition ProtobufWireMaterializer.trailingBytesMessage
                else
                    Ok(struct (ProtobufWireMaterializer.materializeField input parsedField, nextPosition)))

    /// Parses one Protocol Buffers wire message into owned fields collected through end of input.
    let message = ProtobufWireMaterializer.message

    /// Parses one Protocol Buffers wire field and requires the input to end at that field boundary.
    let tryParseField (input: ReadOnlySpan<byte>) : ParseResult<ProtobufField> =
        Contiguous.run ownedField input

    /// Parses one Protocol Buffers wire field or raises `InvalidDataException` when the input is invalid.
    let parseField (input: ReadOnlySpan<byte>) : ProtobufField =
        match tryParseField input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn

    /// Parses one Protocol Buffers wire message into owned fields collected through end of input.
    let tryParseMessage (input: ReadOnlySpan<byte>) : ParseResult<ProtobufField array> =
        Contiguous.run message input

    /// Parses one Protocol Buffers wire message or raises `InvalidDataException` when the input is invalid.
    let parseMessage (input: ReadOnlySpan<byte>) : ProtobufField array =
        match tryParseMessage input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn
