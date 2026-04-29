---
title: Core sized message
sidebar_position: 2
---

# Core sized message

This example shows `u32be` and `takeSlice` working together on a size-prefixed payload.

```fsharp
open System
open BinaryParsec
open BinaryParsec.Syntax

type SizedMessage =
    {
        Size: uint32
        Payload: ByteSlice
    }

let message : ContiguousParser<SizedMessage> =
    parse {
        let! size = u32be
        let! payload = takeSlice (int size)

        return
            {
                Size = size
                Payload = payload
            }
    }

let input: byte array =
    [|
        0x00uy; 0x00uy; 0x00uy; 0x05uy
        0x68uy; 0x65uy; 0x6Cuy; 0x6Cuy; 0x6Fuy
    |]
```

Observed output:

```text
Size = 5
Payload offset = 4
Payload length = 5
Payload text = hello
```

Source: [examples/core-sized-message.fsx](/source/examples/core-sized-message.fsx)
