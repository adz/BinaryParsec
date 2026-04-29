---
title: PNG initial slice
sidebar_position: 4
---

# PNG initial slice

This example shows `Png.initialSlice` capturing the PNG signature and first chunk without forcing payload materialization.

```fsharp
open System
open BinaryParsec
open BinaryParsec.Protocols.Png

let input: byte array =
    [|
        0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
        0x00uy; 0x00uy; 0x00uy; 0x0Duy
        0x49uy; 0x48uy; 0x44uy; 0x52uy
        0x00uy; 0x00uy; 0x00uy; 0x01uy
        0x00uy; 0x00uy; 0x00uy; 0x01uy
        0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy
        0x90uy; 0x77uy; 0x53uy; 0xDEuy
    |]

let parsed =
    Contiguous.run Png.initialSlice (ReadOnlySpan<byte>(input))
```

Observed output:

```text
Signature length = 8
First chunk type = IHDR
First chunk payload length = 13
```

Source: [examples/png-initial-slice.fsx](/source/examples/png-initial-slice.fsx)
