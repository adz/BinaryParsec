namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Modbus
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module ModbusRtuSliceTests =
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
            0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0xCDuy
        |]

    let private responseFrame =
        [|
            0x11uy; 0x03uy; 0x06uy; 0xAEuy; 0x41uy; 0x56uy; 0x52uy; 0x43uy; 0x40uy; 0x49uy; 0xADuy
        |]

    [<Fact>]
    let ``frame parses address function payload and matching crc`` () =
        match Contiguous.run ModbusRtu.frame (ReadOnlySpan<byte>(requestFrame)) with
        | Ok frame ->
            Assert.Equal(0x01uy, frame.Address)
            Assert.Equal(0x03uy, frame.FunctionCode)
            Assert.Equal(ByteSlice.create 2 4, frame.Payload)
            Assert.Equal<byte>([| 0x00uy; 0x00uy; 0x00uy; 0x0Auy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(requestFrame)) frame.Payload).ToArray())
            Assert.Equal(0xCDC5us, frame.Crc.Expected)
            Assert.Equal(0xCDC5us, frame.Crc.Actual)
            Assert.True(frame.Crc.IsMatch)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``frame allows variable payload lengths within single rtu frame`` () =
        match Contiguous.run ModbusRtu.frame (ReadOnlySpan<byte>(responseFrame)) with
        | Ok frame ->
            Assert.Equal(0x11uy, frame.Address)
            Assert.Equal(0x03uy, frame.FunctionCode)
            Assert.Equal(ByteSlice.create 2 7, frame.Payload)
            Assert.Equal<byte>([| 0x06uy; 0xAEuy; 0x41uy; 0x56uy; 0x52uy; 0x43uy; 0x40uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(responseFrame)) frame.Payload).ToArray())
            Assert.Equal(0xAD49us, frame.Crc.Expected)
            Assert.Equal(0xAD49us, frame.Crc.Actual)
            Assert.True(frame.Crc.IsMatch)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``frame requires address function and crc bytes`` () =
        invoke ModbusRtu.frame [| 0x01uy; 0x03uy; 0xC5uy |] ParsePosition.origin
        |> expectError "Modbus RTU frame must contain address, function code, and CRC." ParsePosition.origin

    [<Fact>]
    let ``frame reports incomplete frame at current offset`` () =
        let input =
            [|
                0xFFuy
                0x01uy; 0x03uy; 0xC5uy
            |]

        invoke ModbusRtu.frame input (ParsePosition.create 1 0)
        |> expectError "Modbus RTU frame must contain address, function code, and CRC." (ParsePosition.create 1 0)

    [<Fact>]
    let ``frame rejects bit offset starts`` () =
        let offset = ParsePosition.create 0 4

        invoke ModbusRtu.frame requestFrame offset
        |> expectError "Byte-aligned primitive cannot run when the cursor is at a bit offset." offset

    [<Fact>]
    let ``frame captures mismatched crc without losing payload slice`` () =
        let corruptedCrcFrame =
            [|
                0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0x00uy
            |]

        match Contiguous.run ModbusRtu.frame (ReadOnlySpan<byte>(corruptedCrcFrame)) with
        | Ok frame ->
            Assert.Equal(0x01uy, frame.Address)
            Assert.Equal(0x03uy, frame.FunctionCode)
            Assert.Equal(ByteSlice.create 2 4, frame.Payload)
            Assert.Equal<byte>([| 0x00uy; 0x00uy; 0x00uy; 0x0Auy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(corruptedCrcFrame)) frame.Payload).ToArray())
            Assert.Equal(0x00C5us, frame.Crc.Expected)
            Assert.Equal(0xCDC5us, frame.Crc.Actual)
            Assert.False(frame.Crc.IsMatch)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))
