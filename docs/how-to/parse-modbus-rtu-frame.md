# Parse a Modbus RTU Frame

Use `BinaryParsec.Protocols.Modbus.ModbusRtu` when application code needs a stable RTU frame parser rather than a low-level parser value.

## Result-based parsing

```fsharp
open BinaryParsec.Protocols.Modbus

match ModbusRtu.TryParseFrame frameBytes with
| Ok frame ->
    printfn "address=%d function=%d payload-bytes=%d" frame.Address frame.FunctionCode frame.Payload.Length
| Error error ->
    printfn "parse failed at byte %d: %s" error.Position.ByteOffset error.Message
```

## Throwing convenience parsing

```fsharp
open BinaryParsec.Protocols.Modbus

let frame = ModbusRtu.ParseFrame frameBytes
```

`ParseFrame` raises `InvalidDataException` when the input does not contain one valid RTU frame with a matching CRC.

## Notes

- `FunctionCode` reports the logical function code.
- `RawFunctionCode` preserves the transmitted function byte.
- `IsExceptionResponse` tells you when the frame is an exception response.
- `ExceptionCode` is only populated for exception responses.
- `Payload` is an owned copy, so the parsed frame does not depend on the original buffer staying alive.
