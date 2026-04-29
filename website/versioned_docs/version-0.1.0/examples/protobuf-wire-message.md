---
title: Protocol Buffers wire message
sidebar_position: 5
---

# Protocol Buffers wire message

This example shows `ProtobufWire.tryParseMessage` collecting a small message through end of input while preserving the wire-field structure.

```fsharp
open System
open BinaryParsec.Protocols.Protobuf

let input: byte array =
    [|
        0x08uy; 0x96uy; 0x01uy
        0x12uy; 0x02uy; 0x6Fuy; 0x6Buy
        0x18uy; 0x2Auy
    |]

let parsed =
    ProtobufWire.tryParseMessage(ReadOnlySpan<byte>(input))
```

Observed output:

```text
Field count = 3
Field 1 wire type = Varint
Field 2 wire type = LengthDelimited
Field 3 number = 3
```

Source: [examples/protobuf-wire-message.fsx](/source/examples/protobuf-wire-message.fsx)
