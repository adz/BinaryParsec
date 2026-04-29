---
title: CAN classic frame
sidebar_position: 3
---

# CAN classic frame

This example shows `CanClassic.tryParseFrame` materializing a validated owned frame from a small controller-header snippet.

```fsharp
open System
open BinaryParsec.Protocols.Can

let input: byte array =
    [|
        0xB4uy; 0x60uy; 0x48uy
    |]

let parsed =
    CanClassic.tryParseFrame(ReadOnlySpan<byte>(input))
```

Observed output:

```text
BaseIdentifier = 1443
IsRemoteTransmissionRequest = true
DataLengthCode = 8
Data length = 0
```

Source: [examples/can-classic-frame.fsx](/source/examples/can-classic-frame.fsx)
