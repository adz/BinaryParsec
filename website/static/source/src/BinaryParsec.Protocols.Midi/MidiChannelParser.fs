namespace BinaryParsec.Protocols.Midi

open BinaryParsec
open BinaryParsec.Syntax

[<RequireQualifiedAccess>]
module internal MidiChannelParser =
    let private deltaTime =
        ContiguousParser<uint32>(fun input startPosition ->
            let mutable currentPosition = startPosition
            let mutable value = 0u
            let mutable count = 0
            let mutable finished = false
            let mutable failure = None

            while not finished do
                match (optional ``byte``).Invoke(input, currentPosition) with
                | Error error ->
                    failure <- Some error.Message
                    finished <- true
                | Ok struct (Some current, nextPosition) ->
                    count <- count + 1
                    if count > 4 then
                        failure <- Some "MIDI delta-time variable-length quantity cannot exceed four bytes."
                        finished <- true
                    else
                        value <- (value <<< 7) ||| uint32 (current &&& 0x7Fuy)
                        currentPosition <- nextPosition
                        if (current &&& 0x80uy) = 0uy then
                            finished <- true
                | Ok struct (None, _) ->
                    failure <- Some "Unexpected end of input while reading MIDI delta-time."
                    finished <- true

            match failure with
            | Some msg -> Contiguous.failAt startPosition msg
            | None -> Contiguous.succeed value currentPosition)

    let private channelStatus =
        parse {
            let! statusPosition = (position)
            let! next = (peekByte)

            if next >= 0x80uy then
                let! status = (``byte``)

                if status < 0xF0uy then
                    return ValueSome status
                else
                    return! fail statusPosition $"MIDI package supports only channel voice status bytes, got 0x{status:X2}."
            else
                return ValueNone
        }

    let private eventData status =
        match status &&& 0xF0uy with
        | 0x90uy ->
            parse {
                let! note = (``byte``)
                let! velocity = (``byte``)

                return
                    {
                        DeltaTime = 0u
                        Status = status
                        Data1 = note
                        Data2 = ValueSome velocity
                    }
            }
        | 0xC0uy ->
            parse {
                let! program = (``byte``)

                return
                    {
                        DeltaTime = 0u
                        Status = status
                        Data1 = program
                        Data2 = ValueNone
                    }
            }
        | _ ->
            let messageType = status &&& 0xF0uy
            fail ParsePosition.origin $"MIDI package currently supports only Note On and Program Change channel events, got status 0x{messageType:X2}."

    /// Parses a MIDI channel-event stream with delta-time VLQs and running status.
    let channelEventStream : ContiguousParser<MidiChannelEventSlice array> =
        ContiguousParser<_>(fun input startPosition ->
            let events = ResizeArray<MidiChannelEventSlice>()
            let mutable currentPosition = startPosition
            let mutable runningStatus = ValueNone
            let mutable failure = None
            let mutable finished = false

            while not finished do
                match (remainingBytes).Invoke(input, currentPosition) with
                | Ok struct (rem, _) when rem = 0 -> finished <- true
                | Error e ->
                    failure <- Some e
                    finished <- true
                | _ ->
                    let eventResult =
                        (parse {
                            let! delta = deltaTime
                            let! eventPos = (position)
                            let! nextStatus = channelStatus

                            let status =
                                match nextStatus, runningStatus with
                                | ValueSome explicitStatus, _ -> explicitStatus
                                | ValueNone, ValueSome previousStatus -> previousStatus
                                | ValueNone, ValueNone -> 0uy

                            if status = 0uy then
                                return! fail eventPos "MIDI running status requires a previous channel status byte."
                            else
                                let! event = eventData status
                                return ({ event with DeltaTime = delta }, status)
                        }).Invoke(input, currentPosition)

                    match eventResult with
                    | Ok struct ((event, status), next) ->
                        events.Add event
                        runningStatus <- ValueSome status
                        currentPosition <- next
                    | Error e ->
                        failure <- Some e
                        finished <- true

            match failure with
            | Some e -> Error e
            | None -> Ok struct (events.ToArray(), currentPosition))
