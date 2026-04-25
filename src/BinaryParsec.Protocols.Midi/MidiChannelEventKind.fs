namespace BinaryParsec.Protocols.Midi

/// Identifies the small channel-event subset currently supported by the MIDI package.
[<RequireQualifiedAccess>]
type MidiChannelEventKind =
    | NoteOn
    | ProgramChange
