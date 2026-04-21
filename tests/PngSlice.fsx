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

let private input =
    [|
        0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
        0x00uy; 0x00uy; 0x00uy; 0x0Duy
        0x49uy; 0x48uy; 0x44uy; 0x52uy
        0x00uy; 0x00uy; 0x00uy; 0x01uy
        0x00uy; 0x00uy; 0x00uy; 0x01uy
        0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy
        0x90uy; 0x77uy; 0x53uy; 0xDEuy
    |]

let signatureParserMatchesExactPngBytes () =
    match Png.signature.Invoke(ReadOnlySpan<byte>(input), ParsePosition.origin) with
    | Ok(struct (slice, position)) ->
        assertEqual "signature slice" (ByteSlice.create 0 8) slice
        assertEqual "signature position" (ParsePosition.create 8 0) position
        assertSequenceEqual
            "signature contents"
            [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]
            (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) slice)
    | Error error ->
        fail $"signature parser matches exact png bytes: expected success, got %A{error}"

let chunkEnvelopeCapturesLengthTypePayloadAndCrcSlices () =
    match Png.chunkEnvelope.Invoke(ReadOnlySpan<byte>(input), ParsePosition.create 8 0) with
    | Ok(struct (chunk, position)) ->
        assertEqual "chunk length" 13u chunk.Length
        assertEqual "chunk type slice" (ByteSlice.create 12 4) chunk.ChunkType
        assertEqual "chunk payload slice" (ByteSlice.create 16 13) chunk.Payload
        assertEqual "chunk crc slice" (ByteSlice.create 29 4) chunk.Crc
        assertEqual "chunk position" (ParsePosition.create 33 0) position
        assertSequenceEqual "chunk type contents" [| 0x49uy; 0x48uy; 0x44uy; 0x52uy |] (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) chunk.ChunkType)
        assertSequenceEqual
            "chunk payload contents"
            [| 0x00uy; 0x00uy; 0x00uy; 0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x01uy; 0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy |]
            (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) chunk.Payload)
        assertSequenceEqual "chunk crc contents" [| 0x90uy; 0x77uy; 0x53uy; 0xDEuy |] (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) chunk.Crc)
    | Error error ->
        fail $"chunk envelope captures length type payload and crc slices: expected success, got %A{error}"

let initialSliceParsesSignatureAndFirstChunk () =
    Contiguous.run Png.initialSlice (ReadOnlySpan<byte>(input))
    |> function
        | Ok parsed ->
            assertEqual "initial slice signature" (ByteSlice.create 0 8) parsed.Signature
            assertEqual "initial slice first chunk length" 13u parsed.FirstChunk.Length
            assertEqual "initial slice first chunk payload" (ByteSlice.create 16 13) parsed.FirstChunk.Payload
        | Error error ->
            fail $"initial slice parses signature and first chunk: expected success, got %A{error}"

let signatureRejectsInvalidMagicBytes () =
    let invalidSignature =
        [|
            0x89uy; 0x50uy; 0x4Euy; 0x46uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
        |]

    expectError
        "signature rejects invalid magic bytes"
        "Input does not start with the PNG file signature."
        ParsePosition.origin
        (Png.signature.Invoke(ReadOnlySpan<byte>(invalidSignature), ParsePosition.origin))

let chunkEnvelopeReportsTruncatedPayloadAtPayloadStart () =
    let truncatedPayload =
        [|
            0x00uy; 0x00uy; 0x00uy; 0x0Duy
            0x49uy; 0x48uy; 0x44uy; 0x52uy
            0x00uy; 0x00uy; 0x00uy; 0x01uy
            0x00uy; 0x00uy; 0x00uy; 0x01uy
            0x08uy; 0x02uy; 0x00uy; 0x00uy
        |]

    expectError
        "chunk envelope reports truncated payload at payload start"
        "Unexpected end of input while reading 13 byte(s)."
        (ParsePosition.create 8 0)
        (Png.chunkEnvelope.Invoke(ReadOnlySpan<byte>(truncatedPayload), ParsePosition.origin))

let chunkEnvelopeRejectsUnsupportedLengthBeforeReadingPayload () =
    let invalidLength =
        [|
            0x7Fuy; 0xFFuy; 0xFFuy; 0xFCuy
            0x49uy; 0x48uy; 0x44uy; 0x52uy
        |]

    expectError
        "chunk envelope rejects unsupported length before reading payload"
        "PNG chunk length exceeds supported contiguous input size."
        ParsePosition.origin
        (Png.chunkEnvelope.Invoke(ReadOnlySpan<byte>(invalidLength), ParsePosition.origin))

let tests =
    [ signatureParserMatchesExactPngBytes
      chunkEnvelopeCapturesLengthTypePayloadAndCrcSlices
      initialSliceParsesSignatureAndFirstChunk
      signatureRejectsInvalidMagicBytes
      chunkEnvelopeReportsTruncatedPayloadAtPayloadStart
      chunkEnvelopeRejectsUnsupportedLengthBeforeReadingPayload ]

for test in tests do
    test ()
