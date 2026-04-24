namespace BinaryParsec.Tests

open BinaryParsec

/// Identifies the tiny subset of MIDI channel events covered by this snippet.
[<RequireQualifiedAccess>]
type MidiChannelEventKind =
    | NoteOn
    | ProgramChange

/// Captures one tiny MIDI channel event with its decoded delta-time and running status.
type MidiChannelEventSnippet =
    {
        DeltaTime: uint32
        Status: byte
        Channel: byte
        Kind: MidiChannelEventKind
        Data1: byte
        Data2: byte voption
    }

[<RequireQualifiedAccess>]
module internal MidiEventSnippet =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let rec private deltaTimeBytes start count value =
        Contiguous.parse {
            let! current = Contiguous.``byte``
            let nextCount = count + 1

            if nextCount > 4 then
                return! failAt start "MIDI delta-time variable-length quantity cannot exceed four bytes."
            else
                let nextValue = (value <<< 7) ||| uint32 (current &&& 0x7Fuy)

                if (current &&& 0x80uy) = 0uy then
                    return nextValue
                else
                    return! deltaTimeBytes start nextCount nextValue
        }

    let private deltaTime =
        Contiguous.parse {
            let! start = Contiguous.position
            return! deltaTimeBytes start 0 0u
        }

    let private channelStatus =
        Contiguous.parse {
            let! statusPosition = Contiguous.position
            let! next = Contiguous.peekByte

            if next >= 0x80uy then
                let! status = Contiguous.``byte``

                if status < 0xF0uy then
                    return ValueSome status
                else
                    return! failAt statusPosition $"MIDI snippet only supports channel voice status bytes, got 0x{status:X2}."
            else
                return ValueNone
        }

    let private eventData status =
        let channel = status &&& 0x0Fuy

        match status &&& 0xF0uy with
        | 0x90uy ->
            Contiguous.parse {
                let! note = Contiguous.``byte``
                let! velocity = Contiguous.``byte``

                return
                    {
                        DeltaTime = 0u
                        Status = status
                        Channel = channel
                        Kind = MidiChannelEventKind.NoteOn
                        Data1 = note
                        Data2 = ValueSome velocity
                    }
            }
        | 0xC0uy ->
            Contiguous.parse {
                let! program = Contiguous.``byte``

                return
                    {
                        DeltaTime = 0u
                        Status = status
                        Channel = channel
                        Kind = MidiChannelEventKind.ProgramChange
                        Data1 = program
                        Data2 = ValueNone
                    }
            }
        | _ ->
            let messageType = status &&& 0xF0uy
            ContiguousParser<_>(fun _ current ->
                Contiguous.failAt current $"MIDI snippet only supports Note On and Program Change channel events, got status 0x{messageType:X2}.")

    let rec private eventStream (runningStatus: byte voption) (events: MidiChannelEventSnippet list) : ContiguousParser<MidiChannelEventSnippet list> =
        Contiguous.parse {
            let! remaining = Contiguous.remainingBytes

            if remaining = 0 then
                return List.rev events
            else
                let! delta = deltaTime
                let! eventPosition = Contiguous.position
                let! nextStatus = channelStatus

                let status =
                    match nextStatus, runningStatus with
                    | ValueSome explicitStatus, _ -> explicitStatus
                    | ValueNone, ValueSome previousStatus -> previousStatus
                    | ValueNone, ValueNone -> 0uy

                if status = 0uy then
                    return! failAt eventPosition "MIDI running status requires a previous channel status byte."
                else
                    let! event = eventData status

                    return!
                        eventStream
                            (ValueSome status)
                            ({ event with DeltaTime = delta } :: events)
        }

    /// Parses a tiny MIDI event stream with delta-time VLQs and running status across channel events.
    let channelEventStream : ContiguousParser<MidiChannelEventSnippet list> =
        eventStream ValueNone []
