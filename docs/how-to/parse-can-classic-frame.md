# Parse a CAN Classic Controller Frame

Use `BinaryParsec.Protocols.Can.CanClassic` when application code needs to parse the common controller-buffer layout for a classic 11-bit CAN frame.

## Result-based parsing

```fsharp
open System
open BinaryParsec.Protocols.Can

let frameBytes = [| 0xB4uy; 0x60uy; 0x03uy; 0x11uy; 0x22uy; 0x33uy |]

match CanClassic.tryParseFrame (ReadOnlySpan frameBytes) with
| Ok frame ->
    printfn "id=0x%03X rtr=%b dlc=%d data-bytes=%d" frame.BaseIdentifier frame.IsRemoteTransmissionRequest frame.DataLengthCode frame.Data.Length
| Error error ->
    printfn "parse failed at byte %d bit %d: %s" error.Position.ByteOffset error.Position.BitOffset error.Message
```

## Throwing convenience parsing

```fsharp
open System
open BinaryParsec.Protocols.Can

let frame = CanClassic.parseFrame (ReadOnlySpan frameBytes)
```

`parseFrame` raises `InvalidDataException` when the input does not contain one valid classic base-format controller frame.

## Notes

- `CanClassic.frame` is the lower-level zero-copy tokenizer when payload boundaries matter more than an owned data copy.
- The current package accepts only base-format classic frames and rejects the controller extended-frame marker.
- Remote frames preserve the transmitted DLC but materialize with an empty `Data` array.
- The package currently targets the compact controller-buffer layout rather than raw destuffed on-wire CAN bits or CAN FD.
