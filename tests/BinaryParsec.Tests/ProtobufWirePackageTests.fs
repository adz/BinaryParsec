namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Protobuf
open Swensen.Unquote
open Xunit

type private TinyWireMessage =
    {
        Id: uint64
        Payload: ByteSlice
    }

[<RequireQualifiedAccess>]
module ProtobufWirePackageTests =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let rec private tinyMessageFields id payload : ContiguousParser<TinyWireMessage> =
        Contiguous.parse {
            let! remaining = Contiguous.remainingBytes

            if remaining = 0 then
                match id, payload with
                | ValueSome parsedId, ValueSome parsedPayload ->
                    return
                        { Id = parsedId
                          Payload = parsedPayload }
                | _ ->
                    let! endPosition = Contiguous.position
                    return! failAt endPosition "Tiny wire message requires both field 1 (id) and field 2 (payload)."
            else
                let! field = ProtobufWire.field

                match field.Tag.Number, field.Value with
                | 1u, ProtobufFieldValueSlice.Varint value ->
                    return! tinyMessageFields (ValueSome value) payload
                | 2u, ProtobufFieldValueSlice.LengthDelimited parsedPayload ->
                    return! tinyMessageFields id (ValueSome parsedPayload)
                | _ ->
                    return! tinyMessageFields id payload
        }

    let private tinyMessage =
        tinyMessageFields ValueNone ValueNone

    [<Fact>]
    let ``field returns zero-copy slice for length-delimited payloads`` () =
        let input = [| 0x12uy; 0x03uy; 0x6Fuy; 0x6Buy; 0x21uy |]

        match Contiguous.run ProtobufWire.field (ReadOnlySpan<byte>(input)) with
        | Ok field ->
            Assert.Equal(2u, field.Tag.Number)
            Assert.Equal(ProtobufWireType.LengthDelimited, field.Tag.WireType)

            match field.Value with
            | ProtobufFieldValueSlice.LengthDelimited payload ->
                Assert.Equal(ByteSlice.create 2 3, payload)
            | _ ->
                raise (Xunit.Sdk.XunitException("Expected a length-delimited field payload."))
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseField materializes a complete varint field`` () =
        let input = [| 0x08uy; 0x96uy; 0x01uy |]

        match ProtobufWire.tryParseField (ReadOnlySpan<byte>(input)) with
        | Ok field ->
            Assert.Equal(1u, field.Tag.Number)
            Assert.Equal(ProtobufWireType.Varint, field.Tag.WireType)

            match field.Value with
            | ProtobufFieldValue.Varint value -> test <@ value = 150UL @>
            | _ -> raise (Xunit.Sdk.XunitException("Expected a varint field payload."))
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseField rejects trailing bytes after a single field`` () =
        let input = [| 0x08uy; 0x01uy; 0x08uy; 0x02uy |]

        match ProtobufWire.tryParseField (ReadOnlySpan<byte>(input)) with
        | Ok field ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{field}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 2 0 @>
            test <@ error.Message = "Protocol Buffers wire field must end immediately after the field payload bytes." @>

    [<Fact>]
    let ``tryParseMessage collects supported fields through end of input`` () =
        let input =
            [|
                0x08uy; 0x96uy; 0x01uy
                0x12uy; 0x02uy; 0x6Fuy; 0x6Buy
                0x18uy; 0x2Auy
            |]

        match ProtobufWire.tryParseMessage (ReadOnlySpan<byte>(input)) with
        | Ok fields ->
            Assert.Equal(3, fields.Length)
            test <@ fields[0].Tag.Number = 1u @>
            test <@ fields[1].Tag.WireType = ProtobufWireType.LengthDelimited @>
            test <@ fields[2].Tag.Number = 3u @>

            match fields[1].Value with
            | ProtobufFieldValue.LengthDelimited payload ->
                Assert.Equal<byte>([| 0x6Fuy; 0x6Buy |], payload)
            | _ ->
                raise (Xunit.Sdk.XunitException("Expected a length-delimited field payload."))
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``field rejects field number zero`` () =
        let input = [| 0x00uy |]

        match Contiguous.run ProtobufWire.field (ReadOnlySpan<byte>(input)) with
        | Ok field ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{field}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "Protocol Buffers field number 0 is invalid." @>

    [<Fact>]
    let ``field rejects unsupported wire types`` () =
        let input = [| 0x1Duy; 0x00uy; 0x00uy; 0x00uy; 0x00uy |]

        match Contiguous.run ProtobufWire.field (ReadOnlySpan<byte>(input)) with
        | Ok field ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{field}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "Protocol Buffers wire type 5 is not supported by this package." @>

    [<Fact>]
    let ``tiny message parser can stay separate from wire-field tokenization while skipping unknown fields`` () =
        let input =
            [|
                0x08uy; 0x96uy; 0x01uy
                0x38uy; 0x2Auy
                0x12uy; 0x02uy; 0x6Fuy; 0x6Buy
                0x4Auy; 0x03uy; 0xAAuy; 0xBBuy; 0xCCuy
            |]

        match Contiguous.run tinyMessage (ReadOnlySpan<byte>(input)) with
        | Ok message ->
            test <@ message.Id = 150UL @>
            Assert.Equal<byte>([| 0x6Fuy; 0x6Buy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) message.Payload).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))
