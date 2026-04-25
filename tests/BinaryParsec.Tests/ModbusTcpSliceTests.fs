namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Modbus
open Xunit

[<RequireQualifiedAccess>]
module ModbusTcpSliceTests =
    let private expectError expectedMessage expectedPosition result =
        match result with
        | Ok(struct (value, position)) ->
            raise (Xunit.Sdk.XunitException($"Expected error, got value %A{value} at %A{position}"))
        | Error error ->
            Assert.Equal(expectedMessage, error.Message)
            Assert.Equal(expectedPosition, error.Position)

    let private invoke (parser: ContiguousParser<'T>) (bytes: byte array) position =
        parser.Invoke(ReadOnlySpan<byte>(bytes), position)

    let private requestFrame =
        [|
            0x12uy; 0x34uy
            0x00uy; 0x00uy
            0x00uy; 0x06uy
            0x11uy
            0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
        |]

    [<Fact>]
    let ``frame tokenizes mbap header and shared pdu boundaries`` () =
        match Contiguous.run ModbusTcpParser.frame (ReadOnlySpan<byte>(requestFrame)) with
        | Ok frame ->
            Assert.Equal(0x1234us, frame.TransactionId)
            Assert.Equal(0x11uy, frame.UnitId)
            Assert.Equal(ByteSlice.create 7 5, frame.Pdu)
            Assert.Equal<byte>([| 0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(requestFrame)) frame.Pdu).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``frame rejects non-modbus protocol identifiers`` () =
        let input =
            [|
                0x12uy; 0x34uy
                0x00uy; 0x01uy
                0x00uy; 0x06uy
                0x11uy
                0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
            |]

        invoke ModbusTcpParser.frame input ParsePosition.origin
        |> expectError "Modbus TCP protocol identifier must be 0, got 0x0001." (ParsePosition.create 2 0)

    [<Fact>]
    let ``frame rejects mbap lengths that do not match the remaining bytes`` () =
        let input =
            [|
                0x12uy; 0x34uy
                0x00uy; 0x00uy
                0x00uy; 0x05uy
                0x11uy
                0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
            |]

        invoke ModbusTcpParser.frame input ParsePosition.origin
        |> expectError "Modbus TCP MBAP length must match the remaining unit identifier plus PDU bytes." (ParsePosition.create 4 0)

    [<Fact>]
    let ``frame requires a unit identifier and one pdu function code byte`` () =
        let input =
            [|
                0x12uy; 0x34uy
                0x00uy; 0x00uy
                0x00uy; 0x01uy
                0x11uy
            |]

        invoke ModbusTcpParser.frame input ParsePosition.origin
        |> expectError "Modbus TCP MBAP length must include the unit identifier and at least one PDU function code byte." (ParsePosition.create 4 0)
