#r "../artifacts/api-docs/BinaryParsec/BinaryParsec.dll"
#r "../artifacts/api-docs/BinaryParsec.Protocols.Modbus/BinaryParsec.Protocols.Modbus.dll"

open System
open BinaryParsec.Protocols.Modbus

let input: byte array =
    [|
        0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0xCDuy
    |]

match ModbusRtu.TryParseFrame(ReadOnlySpan<byte>(input)) with
| Ok frame ->
    printfn "Address = %u" frame.Address
    printfn "FunctionCode = %u" frame.FunctionCode
    printfn "Payload length = %d" frame.Payload.Length
    printfn "CRC match = %b" frame.Crc.IsMatch
| Error error ->
    printfn "Error = %s @ %A" error.Message error.Position
