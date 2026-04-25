# Parse a MIDI Channel Event Stream

Use `BinaryParsec.Protocols.Midi.Midi` when application code needs to parse a byte stream of MIDI channel events with delta-time VLQs and running status.

## Result-based parsing

```fsharp
open System
open BinaryParsec.Protocols.Midi

let bytes =
    [|
        0x00uy
        0x90uy; 0x3Cuy; 0x64uy
        0x81uy; 0x00uy
        0x40uy; 0x00uy
    |]

match Midi.tryParseChannelEventStream (ReadOnlySpan bytes) with
| Ok events ->
    for event in events do
        printfn
            "delta=%d status=0x%02X channel=%d kind=%A data1=%d data2=%A"
            event.DeltaTime
            event.Status
            event.Channel
            event.Kind
            event.Data1
            event.Data2
| Error error ->
    printfn "parse failed at byte %d bit %d: %s" error.Position.ByteOffset error.Position.BitOffset error.Message
```

## Throwing convenience parsing

```fsharp
open System
open BinaryParsec.Protocols.Midi

let events = Midi.parseChannelEventStream (ReadOnlySpan bytes)
```

`parseChannelEventStream` raises `InvalidDataException` when the input is invalid.

## Notes

- `Midi.channelEventStream` is the lower-level tokenizer when raw status bytes and event data matter more than the interpreted event kind.
- The current package supports only channel voice status bytes and currently materializes only `Note On` and `Program Change`.
- Running status is applied automatically across channel events in the parsed stream.
- This package does not yet parse full MIDI files, track chunks, meta events, or system-exclusive data.
