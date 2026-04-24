namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Png
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module PngSliceTests =
    let private expectError expectedMessage expectedPosition result =
        match result with
        | Ok(struct (value, position)) ->
            raise (Xunit.Sdk.XunitException($"Expected error, got value %A{value} at %A{position}"))
        | Error error ->
            Assert.Equal(expectedMessage, error.Message)
            Assert.Equal(expectedPosition, error.Position)

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

    let private chunkedInput =
        [|
            0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
            0x00uy; 0x00uy; 0x00uy; 0x0Duy
            0x49uy; 0x48uy; 0x44uy; 0x52uy
            0x00uy; 0x00uy; 0x00uy; 0x01uy
            0x00uy; 0x00uy; 0x00uy; 0x01uy
            0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy
            0x90uy; 0x77uy; 0x53uy; 0xDEuy
            0x00uy; 0x00uy; 0x00uy; 0x02uy
            0x49uy; 0x44uy; 0x41uy; 0x54uy
            0x78uy; 0x9Cuy
            0x12uy; 0x34uy; 0x56uy; 0x78uy
            0x00uy; 0x00uy; 0x00uy; 0x00uy
            0x49uy; 0x45uy; 0x4Euy; 0x44uy
            0xAEuy; 0x42uy; 0x60uy; 0x82uy
        |]

    [<Fact>]
    let ``signature parser matches exact png bytes`` () =
        match Png.signature.Invoke(ReadOnlySpan<byte>(input), ParsePosition.origin) with
        | Ok(struct (slice, position)) ->
            Assert.Equal(ByteSlice.create 0 8, slice)
            Assert.Equal(ParsePosition.create 8 0, position)
            Assert.Equal<byte>([| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) slice).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``chunk envelope captures length type payload and crc slices`` () =
        match Png.chunkEnvelope.Invoke(ReadOnlySpan<byte>(input), ParsePosition.create 8 0) with
        | Ok(struct (chunk, position)) ->
            Assert.Equal(13u, chunk.Length)
            Assert.Equal(ByteSlice.create 12 4, chunk.ChunkType)
            Assert.Equal(ByteSlice.create 16 13, chunk.Payload)
            Assert.Equal(ByteSlice.create 29 4, chunk.Crc)
            Assert.Equal(ParsePosition.create 33 0, position)
            Assert.Equal<byte>([| 0x49uy; 0x48uy; 0x44uy; 0x52uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) chunk.ChunkType).ToArray())
            Assert.Equal<byte>([| 0x00uy; 0x00uy; 0x00uy; 0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x01uy; 0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) chunk.Payload).ToArray())
            Assert.Equal<byte>([| 0x90uy; 0x77uy; 0x53uy; 0xDEuy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) chunk.Crc).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``initial slice parses signature and first chunk`` () =
        match Contiguous.run Png.initialSlice (ReadOnlySpan<byte>(input)) with
        | Ok parsed ->
            Assert.Equal(ByteSlice.create 0 8, parsed.Signature)
            Assert.Equal(13u, parsed.FirstChunk.Length)
            Assert.Equal(ByteSlice.create 16 13, parsed.FirstChunk.Payload)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``chunk stream walks chunk envelopes through iend`` () =
        match Png.chunkStream.Invoke(ReadOnlySpan<byte>(chunkedInput), ParsePosition.origin) with
        | Ok(struct (parsed, position)) ->
            Assert.Equal(ByteSlice.create 0 8, parsed.Signature)
            Assert.Equal(3, parsed.Chunks.Length)
            Assert.Equal(ParsePosition.create chunkedInput.Length 0, position)

            Assert.Equal(13u, parsed.Chunks[0].Length)
            Assert.Equal(ByteSlice.create 12 4, parsed.Chunks[0].ChunkType)
            Assert.Equal(ByteSlice.create 16 13, parsed.Chunks[0].Payload)

            Assert.Equal(2u, parsed.Chunks[1].Length)
            Assert.Equal<byte>([| 0x49uy; 0x44uy; 0x41uy; 0x54uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(chunkedInput)) parsed.Chunks[1].ChunkType).ToArray())
            Assert.Equal<byte>([| 0x78uy; 0x9Cuy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(chunkedInput)) parsed.Chunks[1].Payload).ToArray())

            Assert.Equal(0u, parsed.Chunks[2].Length)
            Assert.Equal<byte>([| 0x49uy; 0x45uy; 0x4Euy; 0x44uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(chunkedInput)) parsed.Chunks[2].ChunkType).ToArray())
            Assert.Equal(ByteSlice.create 55 0, parsed.Chunks[2].Payload)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``signature rejects invalid magic bytes`` () =
        let invalidSignature =
            [|
                0x89uy; 0x50uy; 0x4Euy; 0x46uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
            |]

        Png.signature.Invoke(ReadOnlySpan<byte>(invalidSignature), ParsePosition.origin)
        |> expectError "Input does not start with the PNG file signature." ParsePosition.origin

    [<Fact>]
    let ``chunk envelope reports truncated payload at payload start`` () =
        let truncatedPayload =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x0Duy
                0x49uy; 0x48uy; 0x44uy; 0x52uy
                0x00uy; 0x00uy; 0x00uy; 0x01uy
                0x00uy; 0x00uy; 0x00uy; 0x01uy
                0x08uy; 0x02uy; 0x00uy; 0x00uy
            |]

        Png.chunkEnvelope.Invoke(ReadOnlySpan<byte>(truncatedPayload), ParsePosition.origin)
        |> expectError "Unexpected end of input while reading 13 byte(s)." (ParsePosition.create 8 0)

    [<Fact>]
    let ``chunk envelope rejects unsupported length before reading payload`` () =
        let invalidLength =
            [|
                0x7Fuy; 0xFFuy; 0xFFuy; 0xFCuy
                0x49uy; 0x48uy; 0x44uy; 0x52uy
            |]

        Png.chunkEnvelope.Invoke(ReadOnlySpan<byte>(invalidLength), ParsePosition.origin)
        |> expectError "PNG chunk length exceeds supported contiguous input size." ParsePosition.origin

    [<Fact>]
    let ``chunk stream reports truncation at the later chunk payload start`` () =
        let truncatedSecondPayload =
            [|
                0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
                0x00uy; 0x00uy; 0x00uy; 0x0Duy
                0x49uy; 0x48uy; 0x44uy; 0x52uy
                0x00uy; 0x00uy; 0x00uy; 0x01uy
                0x00uy; 0x00uy; 0x00uy; 0x01uy
                0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy
                0x90uy; 0x77uy; 0x53uy; 0xDEuy
                0x00uy; 0x00uy; 0x00uy; 0x02uy
                0x49uy; 0x44uy; 0x41uy; 0x54uy
                0x78uy
            |]

        Png.chunkStream.Invoke(ReadOnlySpan<byte>(truncatedSecondPayload), ParsePosition.origin)
        |> expectError "Unexpected end of input while reading 2 byte(s)." (ParsePosition.create 41 0)
