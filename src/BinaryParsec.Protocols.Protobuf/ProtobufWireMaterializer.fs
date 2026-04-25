namespace BinaryParsec.Protocols.Protobuf

open System
open BinaryParsec

[<RequireQualifiedAccess>]
module internal ProtobufWireMaterializer =
    let internal trailingBytesMessage =
        "Protocol Buffers wire field must end immediately after the field payload bytes."

    let materializeField (input: ReadOnlySpan<byte>) (field: ProtobufFieldSlice) =
        let value =
            match field.Value with
            | ProtobufFieldValueSlice.Varint parsed ->
                ProtobufFieldValue.Varint parsed
            | ProtobufFieldValueSlice.LengthDelimited payload ->
                let bytes = ByteSlice.asSpan input payload
                let copied = bytes.ToArray()
                ProtobufFieldValue.LengthDelimited copied

        { Tag = field.Tag
          Value = value }

    let message =
        ContiguousParser<ProtobufField array>(fun input position ->
            let fields = ResizeArray<ProtobufField>()
            let mutable current = position
            let mutable failure = ValueNone

            while current.ByteOffset < input.Length && ValueOption.isNone failure do
                match ProtobufWireParser.field.Invoke(input, current) with
                | Error error ->
                    failure <- ValueSome error
                | Ok(struct (field: ProtobufFieldSlice, nextPosition)) ->
                    fields.Add(materializeField input field)
                    current <- nextPosition

            match failure with
            | ValueSome error -> Error error
            | ValueNone -> Ok(struct (fields.ToArray(), current)))
