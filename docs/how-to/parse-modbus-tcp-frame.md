# Parse a Modbus TCP Frame

Use `BinaryParsec.Protocols.Modbus.ModbusTcp` when application code needs an MBAP-aware Modbus TCP parser with a shared PDU model.

## Result-based parsing

```fsharp
open BinaryParsec.Protocols.Modbus

match ModbusTcp.TryParseFrame frameBytes with
| Ok frame ->
    printfn "transaction=%d unit=%d function=%d payload-bytes=%d"
        frame.TransactionId
        frame.UnitId
        frame.Pdu.FunctionCode
        frame.Pdu.Payload.Length
| Error error ->
    printfn "parse failed at byte %d: %s" error.Position.ByteOffset error.Message
```

## Throwing convenience parsing

```fsharp
open BinaryParsec.Protocols.Modbus

let frame = ModbusTcp.ParseFrame frameBytes
```

`ParseFrame` raises `InvalidDataException` when the MBAP framing fields or shared PDU bytes are invalid.

## Notes

- `TransactionId` and `UnitId` stay on the transport frame model.
- `Pdu` exposes the shared Modbus payload after MBAP framing is removed.
- `Pdu.FunctionCode` reports the logical function code.
- `Pdu.RawFunctionCode` preserves the transmitted function byte.
