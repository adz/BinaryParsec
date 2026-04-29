# Parse a Protocol Buffers Wire Message

Use `BinaryParsec.Protocols.Protobuf` when you want wire-format field access without pulling protobuf schema logic into the core parser layer.

## Parse all supported wire fields

```fsharp
open System
open BinaryParsec.Protocols.Protobuf

let input =
    [|
        0x08uy; 0x96uy; 0x01uy
        0x12uy; 0x02uy; 0x6Fuy; 0x6Buy
        0x18uy; 0x2Auy
    |]

match ProtobufWire.tryParseMessage (ReadOnlySpan<byte>(input)) with
| Ok fields ->
    for field in fields do
        printfn "field %u (%A)" field.Tag.Number field.Tag.WireType
| Error error ->
    printfn "parse failed at %A: %s" error.Position error.Message
```

## Build a tiny message parser on top of `ProtobufWire.field`

The package intentionally keeps message interpretation outside the wire tokenizer. That means you can write a small schema-aware parser without changing the package or the core:

```fsharp
open System
open BinaryParsec
open BinaryParsec.Protocols.Protobuf

type TinyWireMessage =
    {
        Id: uint64
        Payload: ByteSlice
    }

let failAt position message =
    ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

let rec tinyMessageFields id payload =
    Contiguous.parse {
        let! remaining = Contiguous.remainingBytes

        if remaining = 0 then
            match id, payload with
            | ValueSome parsedId, ValueSome parsedPayload ->
                return
                    { Id = parsedId
                      Payload = parsedPayload }
            | _ ->
                let! endPosition = Contiguous.position
                return! failAt endPosition "Tiny wire message requires both field 1 and field 2."
        else
            let! field = ProtobufWire.field

            match field.Tag.Number, field.Value with
            | 1u, ProtobufFieldValueSlice.Varint value ->
                return! tinyMessageFields (ValueSome value) payload
            | 2u, ProtobufFieldValueSlice.LengthDelimited parsedPayload ->
                return! tinyMessageFields id (ValueSome parsedPayload)
            | _ ->
                return! tinyMessageFields id payload
    }

let message = tinyMessageFields ValueNone ValueNone
```

That split is the intended package shape: `ProtobufWire.field` handles wire-format tokenization, and application-specific message rules stay above it.
