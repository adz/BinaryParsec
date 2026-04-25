namespace BinaryParsec.Protocols.Midi

/// Captures one tokenized MIDI channel event before higher-level interpretation.
type MidiChannelEventSlice =
    {
        DeltaTime: uint32
        Status: byte
        Data1: byte
        Data2: byte voption
    }
