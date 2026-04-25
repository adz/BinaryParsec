# MIDI Package Reference

`BinaryParsec.Protocols.Midi` currently exposes a narrow MIDI channel-event package over the contiguous core.

The current scope covers:

- delta-time variable-length quantities
- running status across channel events
- `Note On` channel messages
- `Program Change` channel messages

## Public entry points

- `Midi.channelEventStream`
  Parses a channel-event stream into `MidiChannelEventSlice array`.
- `Midi.tryParseChannelEventStream(ReadOnlySpan<byte>)`
  Returns `ParseResult<MidiChannelEvent array>`.
- `Midi.parseChannelEventStream(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid input.

## Public models

### `MidiChannelEventSlice`

- `DeltaTime`
  The decoded delta-time VLQ for the event.
- `Status`
  The resolved status byte after running-status handling.
- `Data1`
  The first event data byte.
- `Data2`
  The second event data byte when the event shape requires one.

### `MidiChannelEvent`

- `DeltaTime`
  The decoded delta-time VLQ for the event.
- `Status`
  The resolved status byte after running-status handling.
- `Channel`
  The low-nibble MIDI channel number.
- `Kind`
  The interpreted event kind within the current package scope.
- `Data1`
  The first event data byte.
- `Data2`
  The optional second event data byte.
