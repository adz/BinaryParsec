#load "../src/BinaryParsec/BinaryParsec.fs"

open System
open BinaryParsec

let inline private fail message =
    raise (InvalidOperationException(message))

let private assertEqual name expected actual =
    if actual <> expected then
        fail $"%s{name}: expected %A{expected}, got %A{actual}"

let private assertSequenceEqual name (expected: byte array) (actual: ReadOnlySpan<byte>) =
    let actualBytes = actual.ToArray()

    if actualBytes <> expected then
        fail $"%s{name}: expected %A{expected}, got %A{actualBytes}"

let private expectSuccess name expectedValue expectedPosition result =
    match result with
    | Ok(struct (value, position)) ->
        assertEqual $"{name} value" expectedValue value
        assertEqual $"{name} position" expectedPosition position
    | Error error ->
        fail $"%s{name}: expected success, got %A{error}"

let private expectError name expectedMessage expectedPosition result =
    match result with
    | Ok(struct (value, position)) ->
        fail $"%s{name}: expected error, got value %A{value} at %A{position}"
    | Error error ->
        assertEqual $"{name} message" expectedMessage error.Message
        assertEqual $"{name} position" expectedPosition error.Position

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

let frameParsesAddressFunctionPayloadAndMatchingCrc () =
    match Contiguous.run ModbusRtu.frame (ReadOnlySpan<byte>(requestFrame)) with
    | Ok(frame) ->
        assertEqual "request address" 0x01uy frame.Address
        assertEqual "request function code" 0x03uy frame.FunctionCode
        assertEqual "request payload slice" (ByteSlice.create 2 4) frame.Payload
        assertSequenceEqual "request payload bytes" [| 0x00uy; 0x00uy; 0x00uy; 0x0Auy |] (ByteSlice.asSpan (ReadOnlySpan<byte>(requestFrame)) frame.Payload)
        assertEqual "request crc expected" 0xCDC5us frame.Crc.Expected
        assertEqual "request crc actual" 0xCDC5us frame.Crc.Actual
        assertEqual "request crc match" true frame.Crc.IsMatch
    | Error error ->
        fail $"frame parses address function payload and matching crc: expected success, got %A{error}"

let frameAllowsVariablePayloadLengthsWithinSingleRtuFrame () =
    match Contiguous.run ModbusRtu.frame (ReadOnlySpan<byte>(responseFrame)) with
    | Ok(frame) ->
        assertEqual "response address" 0x11uy frame.Address
        assertEqual "response function code" 0x03uy frame.FunctionCode
        assertEqual "response payload slice" (ByteSlice.create 2 7) frame.Payload
        assertSequenceEqual
            "response payload bytes"
            [| 0x06uy; 0xAEuy; 0x41uy; 0x56uy; 0x52uy; 0x43uy; 0x40uy |]
            (ByteSlice.asSpan (ReadOnlySpan<byte>(responseFrame)) frame.Payload)
        assertEqual "response crc expected" 0xAD49us frame.Crc.Expected
        assertEqual "response crc actual" 0xAD49us frame.Crc.Actual
        assertEqual "response crc match" true frame.Crc.IsMatch
    | Error error ->
        fail $"frame allows variable payload lengths within single rtu frame: expected success, got %A{error}"

let frameRequiresAddressFunctionAndCrcBytes () =
    invoke ModbusRtu.frame [| 0x01uy; 0x03uy; 0xC5uy |] ParsePosition.origin
    |> expectError
        "frame requires address function and crc bytes"
        "Modbus RTU frame must contain address, function code, and CRC."
        ParsePosition.origin

let frameReportsIncompleteFrameAtCurrentOffset () =
    let input =
        [|
            0xFFuy
            0x01uy; 0x03uy; 0xC5uy
        |]

    invoke ModbusRtu.frame input (ParsePosition.create 1 0)
    |> expectError
        "frame reports incomplete frame at current offset"
        "Modbus RTU frame must contain address, function code, and CRC."
        (ParsePosition.create 1 0)

let frameRejectsBitOffsetStarts () =
    let offset = ParsePosition.create 0 4

    invoke ModbusRtu.frame requestFrame offset
    |> expectError
        "frame rejects bit offset starts"
        "Byte-aligned primitive cannot run when the cursor is at a bit offset."
        offset

let frameCapturesMismatchedCrcWithoutLosingPayloadSlice () =
    let corruptedCrcFrame =
        [|
            0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0x00uy
        |]

    match Contiguous.run ModbusRtu.frame (ReadOnlySpan<byte>(corruptedCrcFrame)) with
    | Ok(frame) ->
        assertEqual "mismatched crc address" 0x01uy frame.Address
        assertEqual "mismatched crc function code" 0x03uy frame.FunctionCode
        assertEqual "mismatched crc payload slice" (ByteSlice.create 2 4) frame.Payload
        assertSequenceEqual
            "mismatched crc payload bytes"
            [| 0x00uy; 0x00uy; 0x00uy; 0x0Auy |]
            (ByteSlice.asSpan (ReadOnlySpan<byte>(corruptedCrcFrame)) frame.Payload)
        assertEqual "mismatched crc expected" 0x00C5us frame.Crc.Expected
        assertEqual "mismatched crc actual" 0xCDC5us frame.Crc.Actual
        assertEqual "mismatched crc match" false frame.Crc.IsMatch
    | Error error ->
        fail $"frame captures mismatched crc without losing payload slice: expected success, got %A{error}"

let tests =
    [ frameParsesAddressFunctionPayloadAndMatchingCrc
      frameAllowsVariablePayloadLengthsWithinSingleRtuFrame
      frameRequiresAddressFunctionAndCrcBytes
      frameReportsIncompleteFrameAtCurrentOffset
      frameRejectsBitOffsetStarts
      frameCapturesMismatchedCrcWithoutLosingPayloadSlice ]

for test in tests do
    test ()
