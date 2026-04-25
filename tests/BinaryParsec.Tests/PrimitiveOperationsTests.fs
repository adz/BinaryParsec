namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Syntax
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module PrimitiveOperationsTests =
    let private expectSuccess expectedValue expectedPosition result =
        match result with
        | Ok(struct (value, position)) ->
            test <@ value = expectedValue @>
            test <@ position = expectedPosition @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    let private expectFailure expectedPosition expectedMessage result =
        match result with
        | Ok(struct (value, position)) ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got value %A{value} at %A{position}"))
        | Error error ->
            test <@ error.Position = expectedPosition @>
            test <@ error.Message = expectedMessage @>

    let private invoke (parser: ContiguousParser<'T>) (bytes: byte array) position =
        parser.Invoke(ReadOnlySpan<byte>(bytes), position)

    [<Fact>]
    let ``byte reads and advances`` () =
        invoke Contiguous.``byte`` [| 0x2Auy; 0x7Fuy |] ParsePosition.origin
        |> expectSuccess 0x2Auy (ParsePosition.create 1 0)

    [<Fact>]
    let ``peekByte leaves cursor in place`` () =
        invoke Contiguous.peekByte [| 0x2Auy; 0x7Fuy |] ParsePosition.origin
        |> expectSuccess 0x2Auy ParsePosition.origin

    [<Fact>]
    let ``skip advances without returning data`` () =
        invoke (Contiguous.skip 2) [| 0x01uy; 0x02uy; 0x03uy |] ParsePosition.origin
        |> expectSuccess () (ParsePosition.create 2 0)

    [<Fact>]
    let ``take returns slice at current offset`` () =
        let input = [| 0x10uy; 0x20uy; 0x30uy; 0x40uy |]

        match invoke (Contiguous.take 2) input (ParsePosition.create 1 0) with
        | Ok(struct (slice, position)) ->
            test <@ slice = ByteSlice.create 1 2 @>
            test <@ position = ParsePosition.create 3 0 @>
            Assert.Equal<byte>([| 0x20uy; 0x30uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) slice).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``expectBytes matches exact sequence and returns its slice`` () =
        let input = [| 0xAAuy; 0x89uy; 0x50uy; 0x4Euy; 0x47uy |]

        match invoke (Contiguous.expectBytes [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy |] "bad magic") input (ParsePosition.create 1 0) with
        | Ok(struct (slice, position)) ->
            test <@ slice = ByteSlice.create 1 4 @>
            test <@ position = ParsePosition.create 5 0 @>
            Assert.Equal<byte>([| 0x89uy; 0x50uy; 0x4Euy; 0x47uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) slice).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``expectBytes reports mismatch at the starting offset`` () =
        invoke (Contiguous.expectBytes [| 0x89uy; 0x50uy |] "bad magic") [| 0x89uy; 0x51uy |] ParsePosition.origin
        |> expectFailure ParsePosition.origin "bad magic"

    [<Fact>]
    let ``fixed-width integer reads respect endianness`` () =
        invoke Contiguous.u16be [| 0x12uy; 0x34uy |] ParsePosition.origin
        |> expectSuccess 0x1234us (ParsePosition.create 2 0)

        invoke Contiguous.u16le [| 0x12uy; 0x34uy |] ParsePosition.origin
        |> expectSuccess 0x3412us (ParsePosition.create 2 0)

        invoke Contiguous.u32be [| 0x12uy; 0x34uy; 0x56uy; 0x78uy |] ParsePosition.origin
        |> expectSuccess 0x12345678u (ParsePosition.create 4 0)

        invoke Contiguous.u32le [| 0x12uy; 0x34uy; 0x56uy; 0x78uy |] ParsePosition.origin
        |> expectSuccess 0x78563412u (ParsePosition.create 4 0)

        invoke Contiguous.u64be [| 0x01uy; 0x23uy; 0x45uy; 0x67uy; 0x89uy; 0xABuy; 0xCDuy; 0xEFuy |] ParsePosition.origin
        |> expectSuccess 0x0123456789ABCDEFUL (ParsePosition.create 8 0)

        invoke Contiguous.u64le [| 0x01uy; 0x23uy; 0x45uy; 0x67uy; 0x89uy; 0xABuy; 0xCDuy; 0xEFuy |] ParsePosition.origin
        |> expectSuccess 0xEFCDAB8967452301UL (ParsePosition.create 8 0)

    [<Fact>]
    let ``readAt parses at an absolute offset without moving the current cursor`` () =
        let parser = Contiguous.readAt 3 Contiguous.u16be

        invoke parser [| 0x00uy; 0x01uy; 0x02uy; 0x12uy; 0x34uy; 0xFFuy |] (ParsePosition.create 1 3)
        |> expectSuccess 0x1234us (ParsePosition.create 1 3)

    [<Fact>]
    let ``runExact rejects trailing bytes`` () =
        match Contiguous.runExact Contiguous.``byte`` (ReadOnlySpan<byte>([| 0x2Auy; 0x7Fuy |])) with
        | Ok value ->
            raise (Xunit.Sdk.XunitException($"Expected trailing-bytes failure, got %A{value}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 1 0 @>
            test <@ error.Message = "Parser did not consume the full input." @>

    [<Fact>]
    let ``varUInt64 reads multi-byte protobuf varints`` () =
        invoke Contiguous.varUInt64 [| 0xACuy; 0x02uy; 0xFFuy |] ParsePosition.origin
        |> expectSuccess 300UL (ParsePosition.create 2 0)

    [<Fact>]
    let ``takeVarintPrefixed returns the bounded payload slice`` () =
        let input = [| 0x03uy; 0x10uy; 0x20uy; 0x30uy; 0x40uy |]

        match invoke Contiguous.takeVarintPrefixed input ParsePosition.origin with
        | Ok(struct (slice, position)) ->
            test <@ slice = ByteSlice.create 1 3 @>
            test <@ position = ParsePosition.create 4 0 @>
            Assert.Equal<byte>([| 0x10uy; 0x20uy; 0x30uy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) slice).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``parseExactly parses a bounded nested payload and advances past it`` () =
        let parser =
            Contiguous.parse {
                let! size = Contiguous.``byte``
                let! payload = Contiguous.parseExactly (int size) Contiguous.u16be
                let! trailer = Contiguous.``byte``
                return size, payload, trailer
            }

        invoke parser [| 0x02uy; 0x12uy; 0x34uy; 0xFFuy |] ParsePosition.origin
        |> expectSuccess (0x02uy, 0x1234us, 0xFFuy) (ParsePosition.create 4 0)

    [<Fact>]
    let ``parseExactly remaps nested failure offsets to the outer input`` () =
        let parser =
            Contiguous.parse {
                do! Contiguous.skip 1
                return! Contiguous.parseExactly 2 Contiguous.u32be
            }

        invoke parser [| 0xAAuy; 0x12uy; 0x34uy; 0xFFuy |] ParsePosition.origin
        |> expectFailure (ParsePosition.create 1 0) "Unexpected end of input while reading 4 byte(s)."

    [<Fact>]
    let ``parseRemaining parses the rest of the input as one bounded payload`` () =
        let parser =
            Contiguous.parse {
                let! marker = Contiguous.``byte``
                let! payload = Contiguous.parseRemaining Contiguous.u16be
                return marker, payload
            }

        invoke parser [| 0xA5uy; 0x12uy; 0x34uy |] ParsePosition.origin
        |> expectSuccess (0xA5uy, 0x1234us) (ParsePosition.create 3 0)

    [<Fact>]
    let ``bit reads advance across byte boundary`` () =
        let input = [| 0b1010_0001uy; 0b0100_0000uy |]

        invoke Contiguous.bit input ParsePosition.origin
        |> expectSuccess true (ParsePosition.create 0 1)

        invoke Contiguous.bit input (ParsePosition.create 0 7)
        |> expectSuccess true (ParsePosition.create 1 0)

        invoke Contiguous.bit input (ParsePosition.create 1 0)
        |> expectSuccess false (ParsePosition.create 1 1)

    [<Fact>]
    let ``bits reads packed fields across byte boundaries`` () =
        let input = [| 0b1011_0100uy; 0b0110_1000uy |]

        invoke (Contiguous.bits 11) input ParsePosition.origin
        |> expectSuccess 0x5A3u (ParsePosition.create 1 3)

        invoke (Contiguous.bits 5) input (ParsePosition.create 1 3)
        |> expectSuccess 0b01000u (ParsePosition.create 2 0)

    [<Fact>]
    let ``bitsLsbFirst reads packed fields across byte boundaries`` () =
        let input = [| 0xEDuy; 0xCDuy; 0x01uy |]

        invoke (Contiguous.bitsLsbFirst 1) input ParsePosition.origin
        |> expectSuccess 1u (ParsePosition.create 0 1)

        invoke (Contiguous.bitsLsbFirst 2) input (ParsePosition.create 0 1)
        |> expectSuccess 2u (ParsePosition.create 0 3)

        invoke (Contiguous.bitsLsbFirst 14) input (ParsePosition.create 0 3)
        |> expectSuccess 0x39BDu (ParsePosition.create 2 1)

    [<Fact>]
    let ``bounds failures report exact offsets`` () =
        invoke Contiguous.``byte`` [||] ParsePosition.origin
        |> expectFailure ParsePosition.origin "Unexpected end of input while reading 1 byte(s)."

        invoke Contiguous.u16be [| 0x12uy |] ParsePosition.origin
        |> expectFailure ParsePosition.origin "Unexpected end of input while reading 2 byte(s)."

        invoke (Contiguous.take 3) [| 0x10uy; 0x20uy; 0x30uy |] (ParsePosition.create 1 0)
        |> expectFailure (ParsePosition.create 1 0) "Unexpected end of input while reading 3 byte(s)."

        invoke Contiguous.bit [| 0x80uy |] (ParsePosition.create 1 0)
        |> expectFailure (ParsePosition.create 1 0) "Unexpected end of input while reading 1 byte(s)."

        invoke (Contiguous.bits 9) [| 0x80uy |] ParsePosition.origin
        |> expectFailure ParsePosition.origin "Unexpected end of input while reading 9 bit(s)."

        invoke (Contiguous.bitsLsbFirst 9) [| 0x80uy |] ParsePosition.origin
        |> expectFailure ParsePosition.origin "Unexpected end of input while reading 9 bit(s)."

        invoke Contiguous.varUInt64 [| 0x80uy |] ParsePosition.origin
        |> expectFailure (ParsePosition.create 1 0) "Unexpected end of input while reading 1 byte(s)."

    [<Fact>]
    let ``varUInt64 rejects values wider than 64 bits`` () =
        invoke Contiguous.varUInt64 [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0x02uy |] ParsePosition.origin
        |> expectFailure (ParsePosition.create 9 0) "Varint exceeds 64 bits."

    [<Fact>]
    let ``byte aligned primitives reject bit offsets`` () =
        let offset = ParsePosition.create 0 3
        let message = "Byte-aligned primitive cannot run when the cursor is at a bit offset."

        invoke Contiguous.``byte`` [| 0xFFuy |] offset
        |> expectFailure offset message

        invoke Contiguous.peekByte [| 0xFFuy |] offset
        |> expectFailure offset message

        invoke (Contiguous.skip 1) [| 0xFFuy |] offset
        |> expectFailure offset message

        invoke (Contiguous.take 1) [| 0xFFuy |] offset
        |> expectFailure offset message

        invoke Contiguous.u16le [| 0xFFuy; 0x00uy |] offset
        |> expectFailure offset message

    [<Fact>]
    let ``map transforms without changing cursor movement`` () =
        let parser = Contiguous.map (fun value -> int value + 1) Contiguous.``byte``

        invoke parser [| 0x2Auy; 0x7Fuy |] ParsePosition.origin
        |> expectSuccess 43 (ParsePosition.create 1 0)

    [<Fact>]
    let ``zip sequences primitive reads`` () =
        let parser = Contiguous.zip Contiguous.``byte`` Contiguous.``byte``

        invoke parser [| 0x12uy; 0x34uy; 0x56uy |] ParsePosition.origin
        |> expectSuccess (0x12uy, 0x34uy) (ParsePosition.create 2 0)

    [<Fact>]
    let ``mergeSources sequences primitive reads into a struct tuple`` () =
        let parser = Contiguous.mergeSources Contiguous.``byte`` Contiguous.u16be

        invoke parser [| 0x12uy; 0x34uy; 0x56uy; 0x78uy |] ParsePosition.origin
        |> expectSuccess (struct (0x12uy, 0x3456us)) (ParsePosition.create 3 0)

    [<Fact>]
    let ``keep helpers preserve requested side`` () =
        let keepLeftParser = Contiguous.keepLeft Contiguous.``byte`` (Contiguous.skip 1)
        let keepRightParser = Contiguous.keepRight (Contiguous.skip 1) Contiguous.``byte``
        let input = [| 0x10uy; 0x20uy; 0x30uy |]

        invoke keepLeftParser input ParsePosition.origin
        |> expectSuccess 0x10uy (ParsePosition.create 2 0)

        invoke keepRightParser input ParsePosition.origin
        |> expectSuccess 0x20uy (ParsePosition.create 2 0)

    [<Fact>]
    let ``computation expression sequences primitives cleanly`` () =
        let parser =
            Contiguous.parse {
                let! marker = Contiguous.``byte``
                do! Contiguous.skip 1
                let! payload = Contiguous.u16be
                return marker, payload
            }

        invoke parser [| 0xA5uy; 0x00uy; 0x12uy; 0x34uy |] ParsePosition.origin
        |> expectSuccess (0xA5uy, 0x1234us) (ParsePosition.create 4 0)

    [<Fact>]
    let ``syntax module supports low ceremony parser definitions`` () =
        let parser =
            parse {
                let! size = u16be
                let! payload = takeSlice (int size)
                let! checksum = ``byte``
                return size, payload, checksum
            }

        let input = [| 0x00uy; 0x02uy; 0xDEuy; 0xADuy; 0xFFuy |]

        match invoke parser input ParsePosition.origin with
        | Ok(struct ((size, payload, checksum), nextPosition)) ->
            test <@ size = 2us @>
            test <@ payload = ByteSlice.create 2 2 @>
            test <@ checksum = 0xFFuy @>
            test <@ nextPosition = ParsePosition.create 5 0 @>
            Assert.Equal<byte>([| 0xDEuy; 0xADuy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) payload).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``syntax module supports bounded nested parsing without manual slice plumbing`` () =
        let payloadParser =
            parse {
                let! command = ``byte``
                let! argument = u16be
                return command, argument
            }

        let messageParser =
            parse {
                let! payloadLength = ``byte``
                let! payload = parseExactly (int payloadLength) payloadParser
                return payloadLength, payload
            }

        invoke messageParser [| 0x03uy; 0x7Euy; 0x12uy; 0x34uy |] ParsePosition.origin
        |> expectSuccess (0x03uy, (0x7Euy, 0x1234us)) (ParsePosition.create 4 0)

    [<Fact>]
    let ``computation expression and! sequences fixed-shape parsers`` () =
        let parser =
            Contiguous.parse {
                let! marker = Contiguous.``byte``
                and! payload = Contiguous.u16be
                return int marker + int payload
            }

        invoke parser [| 0x01uy; 0x12uy; 0x34uy |] ParsePosition.origin
        |> expectSuccess 0x1235 (ParsePosition.create 3 0)

    [<Fact>]
    let ``composition failure reports the later read offset`` () =
        let parser =
            Contiguous.parse {
                do! Contiguous.skip 1
                return! Contiguous.u16be
            }

        invoke parser [| 0xFFuy; 0x12uy |] ParsePosition.origin
        |> expectFailure (ParsePosition.create 1 0) "Unexpected end of input while reading 2 byte(s)."
