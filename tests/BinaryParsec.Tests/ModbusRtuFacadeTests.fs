namespace BinaryParsec.Tests

open System
open System.IO
open BinaryParsec
open BinaryParsec.Protocols.Modbus
open Xunit

[<RequireQualifiedAccess>]
module ModbusRtuFacadeTests =
    let private requestFrame =
        [|
            0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0xCDuy
        |]

    let private responseFrame =
        [|
            0x11uy; 0x03uy; 0x06uy; 0xAEuy; 0x41uy; 0x56uy; 0x52uy; 0x43uy; 0x40uy; 0x49uy; 0xADuy
        |]

    let private exceptionFrame =
        [|
            0x01uy; 0x83uy; 0x02uy; 0xC0uy; 0xF1uy
        |]

    let private malformedExceptionFrame =
        [|
            0x01uy; 0x83uy; 0x02uy; 0x03uy; 0xB1uy; 0x51uy
        |]

    [<Fact>]
    let ``result facade parses request frame`` () =
        match ModbusRtu.TryParseFrame(ReadOnlySpan<byte>(requestFrame)) with
        | Ok frame ->
            Assert.Equal(0x01uy, frame.Address)
            Assert.Equal(0x03uy, frame.RawFunctionCode)
            Assert.Equal(0x03uy, frame.FunctionCode)
            Assert.Equal<byte>([| 0x00uy; 0x00uy; 0x00uy; 0x0Auy |], frame.Payload)
            Assert.False(frame.IsExceptionResponse)
            Assert.False(frame.ExceptionCode.HasValue)
            Assert.True(frame.Crc.IsMatch)
            Assert.Equal(0xCDC5us, frame.Crc.Expected)
            Assert.Equal(0xCDC5us, frame.Crc.Actual)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``result facade parses regular response frame`` () =
        match ModbusRtu.TryParseFrame(responseFrame) with
        | Ok frame ->
            Assert.Equal(0x11uy, frame.Address)
            Assert.Equal(0x03uy, frame.RawFunctionCode)
            Assert.Equal(0x03uy, frame.FunctionCode)
            Assert.Equal<byte>([| 0x06uy; 0xAEuy; 0x41uy; 0x56uy; 0x52uy; 0x43uy; 0x40uy |], frame.Payload)
            Assert.False(frame.IsExceptionResponse)
            Assert.False(frame.ExceptionCode.HasValue)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``result facade parses exception response and normalizes function code`` () =
        match ModbusRtu.TryParseFrame(exceptionFrame) with
        | Ok frame ->
            Assert.Equal(0x01uy, frame.Address)
            Assert.Equal(0x83uy, frame.RawFunctionCode)
            Assert.Equal(0x03uy, frame.FunctionCode)
            Assert.True(frame.IsExceptionResponse)
            Assert.True(frame.ExceptionCode.HasValue)
            Assert.Equal(0x02uy, frame.ExceptionCode.Value)
            Assert.Equal<byte>([| 0x02uy |], frame.Payload)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``result facade rejects crc mismatch`` () =
        let corruptedCrcFrame =
            [|
                0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0x00uy
            |]

        match ModbusRtu.TryParseFrame(corruptedCrcFrame) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            Assert.Equal(ParsePosition.create 6 0, error.Position)
            Assert.Equal("Modbus RTU CRC mismatch. Expected 0x00C5, computed 0xCDC5.", error.Message)

    [<Fact>]
    let ``result facade rejects malformed exception payload`` () =
        match ModbusRtu.TryParseFrame(malformedExceptionFrame) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            Assert.Equal(ParsePosition.create 2 0, error.Position)
            Assert.Equal("Modbus RTU exception response payload must contain exactly one exception code byte.", error.Message)

    [<Fact>]
    let ``result facade keeps owned payload after caller mutates source buffer`` () =
        let bytes = Array.copy requestFrame

        let frame =
            match ModbusRtu.TryParseFrame(bytes) with
            | Ok parsed -> parsed
            | Error error -> raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

        bytes[2] <- 0xFFuy
        bytes[3] <- 0xFFuy

        Assert.Equal<byte>([| 0x00uy; 0x00uy; 0x00uy; 0x0Auy |], frame.Payload)

    [<Fact>]
    let ``out parameter facade supports csharp style consumption`` () =
        let mutable frame = Unchecked.defaultof<ModbusRtuFrame>
        let mutable error = Unchecked.defaultof<ParseError>

        let ok = ModbusRtu.TryParseFrame(ReadOnlySpan<byte>(requestFrame), &frame, &error)

        Assert.True(ok)
        Assert.Equal(0x01uy, frame.Address)
        Assert.True(obj.ReferenceEquals(error, null))

    [<Fact>]
    let ``throwing facade raises invalid data exception with parse position`` () =
        let exn =
            Assert.Throws<InvalidDataException>(fun () ->
                ModbusRtu.ParseFrame(malformedExceptionFrame) |> ignore)

        Assert.Equal("Modbus RTU exception response payload must contain exactly one exception code byte.", exn.Message)
        Assert.Equal(ParsePosition.create 2 0, exn.Data["ParsePosition"] :?> ParsePosition)
