---
slug: /model/modbus
title: Modbus package reference
sidebar_position: 2
---

# Modbus package reference

`BinaryParsec.Protocols.Modbus` exposes transport-specific entry points over one shared Modbus PDU layer.

## What this shows

This package keeps the transport framing and the payload interpretation separate. `ModbusRtu` and `ModbusTcp` own the frame-level facades, while `ModbusPduParser` keeps the shared payload boundary in one place.

## Core shape

- `ModbusRtu`
- `ModbusTcp`
- `ModbusPdu`
- `ModbusRtuFrame`
- `ModbusTcpFrame`

The core user story is simple: parse a transport frame, normalize the PDU, and keep the transport-specific details in the frame model.

## What you can do

- Parse RTU and TCP frames from contiguous byte input.
- Use `TryParseFrame` and `ParseFrame` overloads from F# or C#.
- Inspect raw PDU bytes without making the core guess the transport policy.
- Preserve exception-response normalization in one place.

## Member map

### Create and run

- `ModbusRtu.TryParseFrame`
- `ModbusRtu.ParseFrame`
- `ModbusTcp.TryParseFrame`
- `ModbusTcp.ParseFrame`

### Transport framing

- `ModbusRtuFrame`
- `ModbusTcpFrame`
- `ModbusRtuCrcResult`

### Shared payloads

- `ModbusPdu`
- `ModbusPduParser`

### Runtime helpers

- `ParseResult<'T>`
- `ParseError`
- `ByteSlice`

### Interop

- `TryParseFrame(ReadOnlySpan<byte>, byref<...>, byref<ParseError>)`
- `TryParseFrame(byte[], byref<...>, byref<ParseError>)`

## Read next

- [Modbus RTU frame how-to](../how-to/parse-modbus-rtu-frame.md)
- [Modbus TCP frame how-to](../how-to/parse-modbus-tcp-frame.md)
- [Modbus package shape explanation](../explanation/modbus-package-shape.md)
- [Modbus authoritative sources](modbus-authoritative-sources.md)

## Source links

- [ModbusRtu.fs](/source/src/BinaryParsec.Protocols.Modbus/ModbusRtu.fs)
- [ModbusTcp.fs](/source/src/BinaryParsec.Protocols.Modbus/ModbusTcp.fs)
- [ModbusPduParser.fs](/source/src/BinaryParsec.Protocols.Modbus/ModbusPduParser.fs)
