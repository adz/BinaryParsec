---
title: MIDI running status
sidebar_position: 8
---

# MIDI running status

This example shows `Midi.tryParseChannelEventStream` preserving running status while materializing interpreted channel events.

```fsharp
open System
open BinaryParsec.Protocols.Midi

let input: byte array =
    [|
        0x00uy
        0xC2uy; 0x05uy
        0x10uy
        0x07uy
    |]

let parsed =
    Midi.tryParseChannelEventStream(ReadOnlySpan<byte>(input))
```

Observed output:

```text
Event count = 2
First kind = ProgramChange
Second kind = ProgramChange
Running status reused = true
```

Source: [examples/midi-running-status.fsx](/source/examples/midi-running-status.fsx)
