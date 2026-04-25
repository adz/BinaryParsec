namespace BinaryParsec.Protocols.Midi

open System
open System.IO
open BinaryParsec

/// MIDI channel-event parsing over the contiguous core.
///
/// The current package intentionally stays narrow. It focuses on delta-time
/// VLQs, running status, and a small channel-event subset rather than the full
/// MIDI file or live-stream surface.
[<RequireQualifiedAccess>]
module Midi =
    /// Parses a MIDI channel-event stream into tokenized event slices.
    let channelEventStream = MidiChannelParser.channelEventStream

    let private ownedChannelEventStream =
        ContiguousParser<MidiChannelEvent array>(fun input position ->
            match channelEventStream.Invoke(input, position) with
            | Error error -> Error error
            | Ok(struct (events, nextPosition)) ->
                Ok(struct (MidiChannelMaterializer.materializeEventStream events, nextPosition)))

    /// Parses a MIDI channel-event stream into interpreted owned events.
    let tryParseChannelEventStream (input: ReadOnlySpan<byte>) : ParseResult<MidiChannelEvent array> =
        Contiguous.run ownedChannelEventStream input

    /// Parses a MIDI channel-event stream or raises `InvalidDataException` when the input is invalid.
    let parseChannelEventStream (input: ReadOnlySpan<byte>) : MidiChannelEvent array =
        match tryParseChannelEventStream input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn
