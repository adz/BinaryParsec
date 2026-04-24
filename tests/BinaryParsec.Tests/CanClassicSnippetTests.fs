namespace BinaryParsec.Tests

open System
open BinaryParsec
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module CanClassicSnippetTests =
    [<Fact>]
    let ``header reads packed identifier flags and dlc`` () =
        let input = [| 0xB4uy; 0x60uy; 0x48uy |]

        match Contiguous.run CanClassicSnippet.header (ReadOnlySpan<byte>(input)) with
        | Ok header ->
            test <@ header.BaseIdentifier = 0x5A3us @>
            test <@ not header.IsExtendedFrame @>
            test <@ header.IsRemoteTransmissionRequest @>
            test <@ header.DataLengthCode = 8uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``header surfaces extended-frame flag from packed sidl bits`` () =
        let input = [| 0xB4uy; 0x68uy; 0x02uy |]

        match Contiguous.run CanClassicSnippet.header (ReadOnlySpan<byte>(input)) with
        | Ok header ->
            test <@ header.BaseIdentifier = 0x5A3us @>
            test <@ header.IsExtendedFrame @>
            test <@ not header.IsRemoteTransmissionRequest @>
            test <@ header.DataLengthCode = 2uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``header rejects classic dlc values above eight`` () =
        let input = [| 0xB4uy; 0x60uy; 0x09uy |]

        match Contiguous.run CanClassicSnippet.header (ReadOnlySpan<byte>(input)) with
        | Ok header ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{header}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 2 0 @>
            test <@ error.Message = "CAN classic DLC must be between 0 and 8, got 9." @>
