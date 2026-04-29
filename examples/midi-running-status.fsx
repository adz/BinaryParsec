#r "../artifacts/api-docs/BinaryParsec/BinaryParsec.dll"
#r "../artifacts/api-docs/BinaryParsec.Protocols.Midi/BinaryParsec.Protocols.Midi.dll"

open System
open BinaryParsec.Protocols.Midi

let input: byte array =
    [|
        0x00uy
        0xC2uy; 0x05uy
        0x10uy
        0x07uy
    |]

match Midi.tryParseChannelEventStream(ReadOnlySpan<byte>(input)) with
| Ok events ->
    printfn "Event count = %d" events.Length
    printfn "First kind = %A" events[0].Kind
    printfn "Second kind = %A" events[1].Kind
    printfn "Running status reused = %b" (events[0].Status = events[1].Status)
| Error error ->
    printfn "Error = %s @ %A" error.Message error.Position
