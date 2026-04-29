#r "../artifacts/api-docs/BinaryParsec/BinaryParsec.dll"
#r "../artifacts/api-docs/BinaryParsec.Protocols.Protobuf/BinaryParsec.Protocols.Protobuf.dll"

open System
open BinaryParsec.Protocols.Protobuf

let input: byte array =
    [|
        0x08uy; 0x96uy; 0x01uy
        0x12uy; 0x02uy; 0x6Fuy; 0x6Buy
        0x18uy; 0x2Auy
    |]

match ProtobufWire.tryParseMessage(ReadOnlySpan<byte>(input)) with
| Ok fields ->
    printfn "Field count = %d" fields.Length
    printfn "Field 1 wire type = %A" fields[0].Tag.WireType
    printfn "Field 2 wire type = %A" fields[1].Tag.WireType
    printfn "Field 3 number = %u" fields[2].Tag.Number
| Error error ->
    printfn "Error = %s @ %A" error.Message error.Position
