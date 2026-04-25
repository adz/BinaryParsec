namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Can
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module CanClassicPackageTests =
    [<Fact>]
    let ``frame returns zero-copy slice for a classic data frame`` () =
        let input = [| 0xB4uy; 0x60uy; 0x08uy; 1uy; 2uy; 3uy; 4uy; 5uy; 6uy; 7uy; 8uy |]

        match Contiguous.run CanClassic.frame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            Assert.Equal(0x5A3us, frame.BaseIdentifier)
            Assert.False(frame.IsExtendedFrame)
            Assert.False(frame.IsRemoteTransmissionRequest)
            Assert.Equal(8uy, frame.DataLengthCode)
            Assert.Equal(ByteSlice.create 3 8, frame.Payload)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseFrame materializes a classic data frame`` () =
        let input = [| 0xB4uy; 0x60uy; 0x03uy; 0x11uy; 0x22uy; 0x33uy |]

        match CanClassic.tryParseFrame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            test <@ frame.BaseIdentifier = 0x5A3us @>
            test <@ not frame.IsRemoteTransmissionRequest @>
            test <@ frame.DataLengthCode = 3uy @>
            test <@ frame.Data = [| 0x11uy; 0x22uy; 0x33uy |] @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseFrame accepts remote frame without payload bytes`` () =
        let input = [| 0xB4uy; 0x60uy; 0x48uy |]

        match CanClassic.tryParseFrame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            test <@ frame.BaseIdentifier = 0x5A3us @>
            test <@ frame.IsRemoteTransmissionRequest @>
            test <@ frame.DataLengthCode = 8uy @>
            test <@ frame.Data = [||] @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseFrame rejects extended marker in controller header`` () =
        let input = [| 0xB4uy; 0x68uy; 0x02uy; 0xAAuy; 0xBBuy |]

        match CanClassic.tryParseFrame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            let expectedMessage =
                "CAN classic package supports only base-format controller frames; the extended-frame marker is not supported."

            test <@ error.Position = ParsePosition.create 1 4 @>
            test <@ error.Message = expectedMessage @>

    [<Fact>]
    let ``tryParseFrame rejects classic dlc values above eight`` () =
        let input = [| 0xB4uy; 0x60uy; 0x09uy |]

        match CanClassic.tryParseFrame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 2 4 @>
            test <@ error.Message = "CAN classic DLC must be between 0 and 8, got 9." @>

    [<Fact>]
    let ``tryParseFrame rejects trailing bytes after payload`` () =
        let input = [| 0xB4uy; 0x60uy; 0x01uy; 0xABuy; 0xCDuy |]

        match CanClassic.tryParseFrame (ReadOnlySpan<byte>(input)) with
        | Ok frame ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{frame}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 4 0 @>
            test <@ error.Message = "CAN classic frame must end immediately after the controller payload bytes." @>
