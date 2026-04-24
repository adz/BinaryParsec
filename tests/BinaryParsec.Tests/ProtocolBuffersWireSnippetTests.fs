namespace BinaryParsec.Tests

open System
open BinaryParsec
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module ProtocolBuffersWireSnippetTests =
    [<Fact>]
    let ``message reads id and payload while skipping unknown varint and length fields`` () =
        let input =
            [|
                0x08uy; 0x96uy; 0x01uy
                0x38uy; 0x2Auy
                0x12uy; 0x02uy; 0x6Fuy; 0x6Buy
                0x4Auy; 0x03uy; 0xAAuy; 0xBBuy; 0xCCuy
            |]

        match Contiguous.run ProtocolBuffersWireSnippet.message (ReadOnlySpan<byte>(input)) with
        | Ok message ->
            test <@ message.Id = 150UL @>
            Assert.Equal<byte>([| 0x6Fuy; 0x6Buy |], (ByteSlice.asSpan (ReadOnlySpan<byte>(input)) message.Payload).ToArray())
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``message rejects field number zero`` () =
        let input = [| 0x00uy |]

        match Contiguous.run ProtocolBuffersWireSnippet.message (ReadOnlySpan<byte>(input)) with
        | Ok message ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{message}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "Protocol Buffers field number 0 is invalid." @>

    [<Fact>]
    let ``message rejects unsupported wire types`` () =
        let input = [| 0x1Duy; 0x00uy; 0x00uy; 0x00uy; 0x00uy |]

        match Contiguous.run ProtocolBuffersWireSnippet.message (ReadOnlySpan<byte>(input)) with
        | Ok message ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{message}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "Protocol Buffers wire type 5 is not supported by this snippet." @>

    [<Fact>]
    let ``message requires both id and payload fields`` () =
        let input = [| 0x08uy; 0x01uy |]

        match Contiguous.run ProtocolBuffersWireSnippet.message (ReadOnlySpan<byte>(input)) with
        | Ok message ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{message}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 2 0 @>
            test <@ error.Message = "Protocol Buffers snippet requires both field 1 (id) and field 2 (payload)." @>
