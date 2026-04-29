#r "../artifacts/api-docs/BinaryParsec/BinaryParsec.dll"
#r "../artifacts/api-docs/BinaryParsec.Protocols.Can/BinaryParsec.Protocols.Can.dll"

open System
open BinaryParsec
open BinaryParsec.Protocols.Can

let input: byte array =
    [|
        0xB4uy; 0x60uy; 0x48uy
    |]

match CanClassic.tryParseFrame(ReadOnlySpan<byte>(input)) with
| Ok frame ->
    printfn "BaseIdentifier = %u" frame.BaseIdentifier
    printfn "IsRemoteTransmissionRequest = %b" frame.IsRemoteTransmissionRequest
    printfn "DataLengthCode = %u" frame.DataLengthCode
    printfn "Data length = %d" frame.Data.Length
| Error error ->
    printfn "Error = %s @ %A" error.Message error.Position
