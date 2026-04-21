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

let tests =
    [ frameParsesAddressFunctionPayloadAndMatchingCrc
      frameAllowsVariablePayloadLengthsWithinSingleRtuFrame ]

for test in tests do
    test ()
