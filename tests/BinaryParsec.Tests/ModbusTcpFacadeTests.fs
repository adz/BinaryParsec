namespace BinaryParsec.Tests

open System
open System.IO
open BinaryParsec
open BinaryParsec.Protocols.Modbus
open Xunit

[<RequireQualifiedAccess>]
module ModbusTcpFacadeTests =
    let private requestFrame =
        [|
            0x12uy; 0x34uy
            0x00uy; 0x00uy
            0x00uy; 0x06uy
            0x11uy
            0x03uy; 0x00uy; 0x6Buy; 0x00uy; 0x03uy
        |]

    let private exceptionFrame =
        [|
            0x00uy; 0x2Auy
            0x00uy; 0x00uy
            0x00uy; 0x03uy
            0x11uy
            0x83uy; 0x02uy
        |]

    let private malformedExceptionFrame =
        [|
            0x00uy; 0x2Auy
            0x00uy; 0x00uy
            0x00uy; 0x04uy
            0x11uy
            0x83uy; 0x02uy; 0x03uy
        |]

    [<Fact>]
    let ``result facade parses mbap transport header and shared pdu`` () =
        match ModbusTcp.TryParseFrame(ReadOnlySpan<byte>(requestFrame)) with
        | Ok frame ->
            Assert.Equal(0x1234us, frame.TransactionId)
            Assert.Equal(0x11uy, frame.UnitId)
            Assert.Equal(0x03uy, frame.Pdu.RawFunctionCode)
            Assert.Equal(0x03uy, frame.Pdu.FunctionCode)
            Assert.Equal<byte>([| 0x00uy; 0x6Buy; 0x00uy; 0x03uy |], frame.Pdu.Payload)
            Assert.False(frame.Pdu.IsExceptionResponse)
            Assert.False(frame.Pdu.ExceptionCode.HasValue)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``result facade parses exception response and normalizes function code`` () =
        match ModbusTcp.TryParseFrame(exceptionFrame) with
        | Ok frame ->
            Assert.Equal(0x002Aus, frame.TransactionId)
            Assert.Equal(0x11uy, frame.UnitId)
            Assert.True(frame.Pdu.IsExceptionResponse)
            Assert.Equal(0x83uy, frame.Pdu.RawFunctionCode)
            Assert.Equal(0x03uy, frame.Pdu.FunctionCode)
            Assert.True(frame.Pdu.ExceptionCode.HasValue)
            Assert.Equal(0x02uy, frame.Pdu.ExceptionCode.Value)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``result facade rejects malformed exception payload`` () =
        match ModbusTcp.TryParseFrame(malformedExceptionFrame) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            Assert.Equal(ParsePosition.create 8 0, error.Position)
            Assert.Equal("Modbus TCP exception response payload must contain exactly one exception code byte.", error.Message)

    [<Fact>]
    let ``out parameter facade supports csharp style consumption`` () =
        let mutable frame = Unchecked.defaultof<ModbusTcpFrame>
        let mutable error = Unchecked.defaultof<ParseError>

        let ok = ModbusTcp.TryParseFrame(ReadOnlySpan<byte>(requestFrame), &frame, &error)

        Assert.True(ok)
        Assert.Equal(0x1234us, frame.TransactionId)
        Assert.True(obj.ReferenceEquals(error, null))

    [<Fact>]
    let ``throwing facade raises invalid data exception with parse position`` () =
        let exn =
            Assert.Throws<InvalidDataException>(fun () ->
                ModbusTcp.ParseFrame(malformedExceptionFrame) |> ignore)

        Assert.Equal("Modbus TCP exception response payload must contain exactly one exception code byte.", exn.Message)
        Assert.Equal(ParsePosition.create 8 0, exn.Data["ParsePosition"] :?> ParsePosition)
