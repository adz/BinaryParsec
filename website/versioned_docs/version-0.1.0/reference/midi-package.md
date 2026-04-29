---
slug: /model/midi
title: MIDI package reference
sidebar_position: 6
---

# MIDI package reference

`BinaryParsec.Protocols.Midi` exposes a narrow channel-event package over the contiguous core.

## What this shows

The package keeps the delta-time parser, running-status handling, and event interpretation separate enough to stay readable while still mapping to a real channel-event stream.

## Core shape

- `Midi`
- `MidiChannelEventSlice`
- `MidiChannelEvent`
- `MidiChannelEventKind`

## What you can do

- Parse channel-event streams with running status.
- Materialize an owned event array when the caller wants interpreted values.
- Keep the stream parser and the event interpretation boundary separate.

## Member map

### Create and run

- `Midi.channelEventStream`
- `Midi.tryParseChannelEventStream`
- `Midi.parseChannelEventStream`

### Runtime helpers

- `MidiChannelParser`
- `MidiChannelMaterializer`

### Collections

- `MidiChannelEventSlice[]`
- `MidiChannelEvent[]`

## Read next

- [Parse a MIDI channel event stream](../how-to/parse-midi-channel-event-stream.md)
- [MIDI package shape explanation](../explanation/midi-package-shape.md)
- [MIDI authoritative sources](midi-authoritative-sources.md)

## Source links

- [Midi.fs](/source/src/BinaryParsec.Protocols.Midi/Midi.fs)
- [MidiChannelParser.fs](/source/src/BinaryParsec.Protocols.Midi/MidiChannelParser.fs)
- [MidiChannelMaterializer.fs](/source/src/BinaryParsec.Protocols.Midi/MidiChannelMaterializer.fs)
