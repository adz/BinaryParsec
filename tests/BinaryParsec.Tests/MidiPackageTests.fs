namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Midi
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module MidiPackageTests =
    [<Fact>]
    let ``channelEventStream reuses running status across note-on events`` () =
        let input =
            [|
                0x00uy
                0x90uy; 0x3Cuy; 0x64uy
                0x81uy; 0x00uy
                0x40uy; 0x00uy
            |]

        match Contiguous.run Midi.channelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            test <@ events.Length = 2 @>

            let first = events[0]
            test <@ first.DeltaTime = 0u @>
            test <@ first.Status = 0x90uy @>
            test <@ first.Data1 = 0x3Cuy @>
            test <@ first.Data2 = ValueSome 0x64uy @>

            let second = events[1]
            test <@ second.DeltaTime = 128u @>
            test <@ second.Status = 0x90uy @>
            test <@ second.Data1 = 0x40uy @>
            test <@ second.Data2 = ValueSome 0x00uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseChannelEventStream materializes program changes with running status`` () =
        let input =
            [|
                0x00uy
                0xC2uy; 0x05uy
                0x10uy
                0x07uy
            |]

        match Midi.tryParseChannelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            test <@ events.Length = 2 @>

            let first = events[0]
            test <@ first.DeltaTime = 0u @>
            test <@ first.Status = 0xC2uy @>
            test <@ first.Channel = 2uy @>
            test <@ first.Kind = MidiChannelEventKind.ProgramChange @>
            test <@ first.Data1 = 0x05uy @>
            test <@ first.Data2 = ValueNone @>

            let second = events[1]
            test <@ second.DeltaTime = 16u @>
            test <@ second.Status = 0xC2uy @>
            test <@ second.Channel = 2uy @>
            test <@ second.Kind = MidiChannelEventKind.ProgramChange @>
            test <@ second.Data1 = 0x07uy @>
            test <@ second.Data2 = ValueNone @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``tryParseChannelEventStream rejects running-status data before any channel status`` () =
        let input = [| 0x00uy; 0x3Cuy; 0x64uy |]

        match Midi.tryParseChannelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{events}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 1 0 @>
            test <@ error.Message = "MIDI running status requires a previous channel status byte." @>

    [<Fact>]
    let ``tryParseChannelEventStream rejects delta-time vlqs wider than four bytes`` () =
        let input = [| 0x81uy; 0x80uy; 0x80uy; 0x80uy; 0x00uy |]

        match Midi.tryParseChannelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{events}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "MIDI delta-time variable-length quantity cannot exceed four bytes." @>

    [<Fact>]
    let ``tryParseChannelEventStream rejects system status bytes in the current package scope`` () =
        let input = [| 0x00uy; 0xF0uy |]

        match Midi.tryParseChannelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{events}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 1 0 @>
            test <@ error.Message = "MIDI package supports only channel voice status bytes, got 0xF0." @>
