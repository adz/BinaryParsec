namespace BinaryParsec.Tests

open System
open BinaryParsec
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module ModbusTcpMbapSnippetTests =
    [<Fact>]
    let ``shared pdu parser stays transport-agnostic`` () =
        let input = [| 0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy |]

        match Contiguous.run ModbusTcpMbapSnippet.pdu (ReadOnlySpan<byte>(input)) with
        | Ok pdu ->
            test <@ pdu.FunctionCode = 0x03uy @>
            test <@ pdu.Data = ByteSlice.create 1 4 @>
            Assert.Equal<byte>([| 0x00uy; 0x6Buy; 0x00uy; 0x03uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) pdu.Data).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``mbap frame validates transport fields and reuses the shared pdu parser`` () =
        let input =
            [|
                0x12uy; 0x34uy
                0x00uy; 0x00uy
                0x00uy; 0x06uy
                0x11uy
                0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
            |]

        match Contiguous.run ModbusTcpMbapSnippet.frame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            test <@ frame.TransactionId = 0x1234us @>
            test <@ frame.UnitId = 0x11uy @>
            test <@ frame.Payload.FunctionCode = 0x03uy @>
            test <@ frame.Payload.Data = ByteSlice.create 8 4 @>
            Assert.Equal<byte>([| 0x00uy; 0x6Buy; 0x00uy; 0x03uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) frame.Payload.Data).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``mbap frame rejects non-modbus protocol identifiers`` () =
        let input =
            [|
                0x12uy; 0x34uy
                0x00uy; 0x01uy
                0x00uy; 0x06uy
                0x11uy
                0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
            |]

        match Contiguous.run ModbusTcpMbapSnippet.frame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 2 0 @>
            test <@ error.Message = "Modbus TCP protocol identifier must be 0, got 0x0001." @>

    [<Fact>]
    let ``mbap frame rejects length fields that do not match the remaining transport payload`` () =
        let input =
            [|
                0x12uy; 0x34uy
                0x00uy; 0x00uy
                0x00uy; 0x05uy
                0x11uy
                0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
            |]

        match Contiguous.run ModbusTcpMbapSnippet.frame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 4 0 @>
            test <@ error.Message = "Modbus TCP MBAP length must match the remaining unit identifier plus PDU bytes." @>
