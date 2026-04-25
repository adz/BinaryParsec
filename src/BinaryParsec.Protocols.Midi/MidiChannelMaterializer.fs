namespace BinaryParsec.Protocols.Midi

[<RequireQualifiedAccess>]
module internal MidiChannelMaterializer =
    let materializeEvent (slice: MidiChannelEventSlice) : MidiChannelEvent =
        let channel = slice.Status &&& 0x0Fuy

        let kind =
            match slice.Status &&& 0xF0uy with
            | 0x90uy -> MidiChannelEventKind.NoteOn
            | 0xC0uy -> MidiChannelEventKind.ProgramChange
            | unsupported ->
                invalidOp $"Unsupported MIDI channel event materialization for status 0x{unsupported:X2}."

        { DeltaTime = slice.DeltaTime
          Status = slice.Status
          Channel = channel
          Kind = kind
          Data1 = slice.Data1
          Data2 = slice.Data2 }

    let materializeEventStream (events: MidiChannelEventSlice array) : MidiChannelEvent array =
        events |> Array.map materializeEvent
