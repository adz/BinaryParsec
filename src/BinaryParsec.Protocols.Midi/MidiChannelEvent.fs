namespace BinaryParsec.Protocols.Midi

/// Captures one interpreted MIDI channel event with decoded running status and channel.
type MidiChannelEvent =
    {
        DeltaTime: uint32
        Status: byte
        Channel: byte
        Kind: MidiChannelEventKind
        Data1: byte
        Data2: byte voption
    }
