namespace BinaryParsec.Tests

open System
open BinaryParsec
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module MidiEventSnippetTests =
    [<Fact>]
    let ``channel event stream reuses running status across note-on events`` () =
        let input =
            [|
                0x00uy
                0x90uy; 0x3Cuy; 0x64uy
                0x81uy; 0x00uy
                0x40uy; 0x00uy
            |]

        match Contiguous.run MidiEventSnippet.channelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            test <@ List.length events = 2 @>

            let first = List.item 0 events
            test <@ first.DeltaTime = 0u @>
            test <@ first.Status = 0x90uy @>
            test <@ first.Channel = 0uy @>
            test <@ first.Kind = MidiChannelEventKind.NoteOn @>
            test <@ first.Data1 = 0x3Cuy @>
            test <@ first.Data2 = ValueSome 0x64uy @>

            let second = List.item 1 events
            test <@ second.DeltaTime = 128u @>
            test <@ second.Status = 0x90uy @>
            test <@ second.Channel = 0uy @>
            test <@ second.Kind = MidiChannelEventKind.NoteOn @>
            test <@ second.Data1 = 0x40uy @>
            test <@ second.Data2 = ValueSome 0x00uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``channel event stream keeps one-byte data width for running-status program changes`` () =
        let input =
            [|
                0x00uy
                0xC2uy; 0x05uy
                0x10uy
                0x07uy
            |]

        match Contiguous.run MidiEventSnippet.channelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            test <@ List.length events = 2 @>

            let first = List.item 0 events
            test <@ first.DeltaTime = 0u @>
            test <@ first.Status = 0xC2uy @>
            test <@ first.Channel = 2uy @>
            test <@ first.Kind = MidiChannelEventKind.ProgramChange @>
            test <@ first.Data1 = 0x05uy @>
            test <@ first.Data2 = ValueNone @>

            let second = List.item 1 events
            test <@ second.DeltaTime = 16u @>
            test <@ second.Status = 0xC2uy @>
            test <@ second.Channel = 2uy @>
            test <@ second.Kind = MidiChannelEventKind.ProgramChange @>
            test <@ second.Data1 = 0x07uy @>
            test <@ second.Data2 = ValueNone @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``channel event stream rejects running-status data before any channel status`` () =
        let input = [| 0x00uy; 0x3Cuy; 0x64uy |]

        match Contiguous.run MidiEventSnippet.channelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{events}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 1 0 @>
            test <@ error.Message = "MIDI running status requires a previous channel status byte." @>

    [<Fact>]
    let ``channel event stream rejects delta-time vlqs wider than four bytes`` () =
        let input = [| 0x81uy; 0x80uy; 0x80uy; 0x80uy; 0x00uy |]

        match Contiguous.run MidiEventSnippet.channelEventStream (ReadOnlySpan<byte>(input)) with
        | Ok events ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{events}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "MIDI delta-time variable-length quantity cannot exceed four bytes." @>
