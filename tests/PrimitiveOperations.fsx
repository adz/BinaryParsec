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
    | Ok(value, position) ->
        assertEqual $"{name} value" expectedValue value
        assertEqual $"{name} position" expectedPosition position
    | Error error ->
        fail $"%s{name}: expected success, got %A{error}"

let private expectFailure name expectedPosition expectedMessage result =
    match result with
    | Ok(value, position) ->
        fail $"%s{name}: expected failure, got value %A{value} at %A{position}"
    | Error error ->
        assertEqual $"{name} position" expectedPosition error.Position
        assertEqual $"{name} message" expectedMessage error.Message

let private invoke (parser: ContiguousParser<'T>) (bytes: byte array) position =
    parser.Invoke(ReadOnlySpan<byte>(bytes), position)

let byteReadsAndAdvances () =
    invoke Contiguous.``byte`` [| 0x2Auy; 0x7Fuy |] ParsePosition.origin
    |> expectSuccess "byte reads and advances" 0x2Auy (ParsePosition.create 1 0)

let peekByteLeavesCursorInPlace () =
    invoke Contiguous.peekByte [| 0x2Auy; 0x7Fuy |] ParsePosition.origin
    |> expectSuccess "peekByte leaves cursor in place" 0x2Auy ParsePosition.origin

let skipAdvancesWithoutReturningData () =
    invoke (Contiguous.skip 2) [| 0x01uy; 0x02uy; 0x03uy |] ParsePosition.origin
    |> expectSuccess "skip advances without returning data" () (ParsePosition.create 2 0)

let takeReturnsSliceAtCurrentOffset () =
    let input = [| 0x10uy; 0x20uy; 0x30uy; 0x40uy |]

    match invoke (Contiguous.take 2) input (ParsePosition.create 1 0) with
    | Ok(slice, position) ->
        assertEqual "take slice" (ByteSlice.create 1 2) slice
        assertEqual "take position" (ParsePosition.create 3 0) position
        assertSequenceEqual "take slice contents" [| 0x20uy; 0x30uy |] (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) slice)
    | Error error ->
        fail $"take returns slice at current offset: expected success, got %A{error}"

let u16ReadsRespectEndianness () =
    invoke Contiguous.u16be [| 0x12uy; 0x34uy |] ParsePosition.origin
    |> expectSuccess "u16be reads big-endian" 0x1234us (ParsePosition.create 2 0)

    invoke Contiguous.u16le [| 0x12uy; 0x34uy |] ParsePosition.origin
    |> expectSuccess "u16le reads little-endian" 0x3412us (ParsePosition.create 2 0)

let bitReadsAdvanceAcrossByteBoundary () =
    let input = [| 0b1010_0001uy; 0b0100_0000uy |]

    invoke Contiguous.bit input ParsePosition.origin
    |> expectSuccess "bit 0" true (ParsePosition.create 0 1)

    invoke Contiguous.bit input (ParsePosition.create 0 7)
    |> expectSuccess "bit 7" true (ParsePosition.create 1 0)

    invoke Contiguous.bit input (ParsePosition.create 1 0)
    |> expectSuccess "bit 8" false (ParsePosition.create 1 1)

let boundsFailuresReportExactOffsets () =
    invoke Contiguous.``byte`` [||] ParsePosition.origin
    |> expectFailure
        "byte eof"
        ParsePosition.origin
        "Unexpected end of input while reading 1 byte(s)."

    invoke Contiguous.u16be [| 0x12uy |] ParsePosition.origin
    |> expectFailure
        "u16be truncated"
        ParsePosition.origin
        "Unexpected end of input while reading 2 byte(s)."

    invoke (Contiguous.take 3) [| 0x10uy; 0x20uy; 0x30uy |] (ParsePosition.create 1 0)
    |> expectFailure
        "take truncated"
        (ParsePosition.create 1 0)
        "Unexpected end of input while reading 3 byte(s)."

    invoke Contiguous.bit [| 0x80uy |] (ParsePosition.create 1 0)
    |> expectFailure
        "bit eof"
        (ParsePosition.create 1 0)
        "Unexpected end of input while reading 1 byte(s)."

let byteAlignedPrimitivesRejectBitOffsets () =
    let offset = ParsePosition.create 0 3
    let message = "Byte-aligned primitive cannot run when the cursor is at a bit offset."

    invoke Contiguous.``byte`` [| 0xFFuy |] offset
    |> expectFailure "byte bit-offset guard" offset message

    invoke Contiguous.peekByte [| 0xFFuy |] offset
    |> expectFailure "peekByte bit-offset guard" offset message

    invoke (Contiguous.skip 1) [| 0xFFuy |] offset
    |> expectFailure "skip bit-offset guard" offset message

    invoke (Contiguous.take 1) [| 0xFFuy |] offset
    |> expectFailure "take bit-offset guard" offset message

    invoke Contiguous.u16le [| 0xFFuy; 0x00uy |] offset
    |> expectFailure "u16le bit-offset guard" offset message

let mapTransformsWithoutChangingCursorMovement () =
    let parser =
        Contiguous.map (fun value -> int value + 1) Contiguous.``byte``

    invoke parser [| 0x2Auy; 0x7Fuy |] ParsePosition.origin
    |> expectSuccess "map transforms value" 43 (ParsePosition.create 1 0)

let zipSequencesPrimitiveReads () =
    let parser = Contiguous.zip Contiguous.``byte`` Contiguous.``byte``

    invoke parser [| 0x12uy; 0x34uy; 0x56uy |] ParsePosition.origin
    |> expectSuccess "zip sequences reads" (0x12uy, 0x34uy) (ParsePosition.create 2 0)

let keepHelpersPreserveRequestedSide () =
    let keepLeftParser = Contiguous.keepLeft Contiguous.``byte`` (Contiguous.skip 1)
    let keepRightParser = Contiguous.keepRight (Contiguous.skip 1) Contiguous.``byte``
    let input = [| 0x10uy; 0x20uy; 0x30uy |]

    invoke keepLeftParser input ParsePosition.origin
    |> expectSuccess "keepLeft keeps first value" 0x10uy (ParsePosition.create 2 0)

    invoke keepRightParser input ParsePosition.origin
    |> expectSuccess "keepRight keeps second value" 0x20uy (ParsePosition.create 2 0)

let computationExpressionSequencesPrimitivesCleanly () =
    let parser =
        Contiguous.parse {
            let! marker = Contiguous.``byte``
            do! Contiguous.skip 1
            let! payload = Contiguous.u16be
            return marker, payload
        }

    invoke parser [| 0xA5uy; 0x00uy; 0x12uy; 0x34uy |] ParsePosition.origin
    |> expectSuccess
        "computation expression sequences primitives"
        (0xA5uy, 0x1234us)
        (ParsePosition.create 4 0)

let compositionFailureReportsTheLaterReadOffset () =
    let parser =
        Contiguous.parse {
            do! Contiguous.skip 1
            return! Contiguous.u16be
        }

    invoke parser [| 0xFFuy; 0x12uy |] ParsePosition.origin
    |> expectFailure
        "composition failure offset"
        (ParsePosition.create 1 0)
        "Unexpected end of input while reading 2 byte(s)."

let tests =
    [ byteReadsAndAdvances
      peekByteLeavesCursorInPlace
      skipAdvancesWithoutReturningData
      takeReturnsSliceAtCurrentOffset
      u16ReadsRespectEndianness
      bitReadsAdvanceAcrossByteBoundary
      boundsFailuresReportExactOffsets
      byteAlignedPrimitivesRejectBitOffsets
      mapTransformsWithoutChangingCursorMovement
      zipSequencesPrimitiveReads
      keepHelpersPreserveRequestedSide
      computationExpressionSequencesPrimitivesCleanly
      compositionFailureReportsTheLaterReadOffset ]

for test in tests do
    test ()
