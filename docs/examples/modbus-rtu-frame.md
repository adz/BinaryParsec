---
title: Modbus RTU frame
sidebar_position: 6
---

# Modbus RTU frame

This example shows `ModbusRtu.TryParseFrame` returning a stable owned model with CRC validation and normalized PDU fields.

```fsharp
open System
open BinaryParsec.Protocols.Modbus

let input: byte array =
    [|
        0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0xCDuy
    |]

let parsed =
    ModbusRtu.TryParseFrame(ReadOnlySpan<byte>(input))
```

Observed output:

```text
Address = 1
FunctionCode = 3
Payload length = 4
CRC match = true
```

Source: [examples/modbus-rtu-frame.fsx](/source/examples/modbus-rtu-frame.fsx)
