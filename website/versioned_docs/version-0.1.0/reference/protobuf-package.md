---
slug: /model/protobuf
title: Protocol Buffers package reference
sidebar_position: 8
---

# Protocol Buffers package reference

`BinaryParsec.Protocols.Protobuf` exposes wire-field tokenization plus a wire-message collector over the shared contiguous parser core.

## What this shows

The package stays at the wire-format layer. It parses field tags plus the supported payload encodings and leaves schema-specific message interpretation to higher-level code.

## Core shape

- `ProtobufWire`
- `ProtobufFieldTag`
- `ProtobufFieldSlice`
- `ProtobufFieldValueSlice`
- `ProtobufField`
- `ProtobufFieldValue`

## What you can do

- Parse one wire field as a zero-copy slice.
- Collect repeated wire fields through end of input.
- Materialize owned field payloads only when a caller needs them.

## Member map

### Create and run

- `ProtobufWire.field`
- `ProtobufWire.message`
- `ProtobufWire.tryParseField`
- `ProtobufWire.parseField`
- `ProtobufWire.tryParseMessage`
- `ProtobufWire.parseMessage`

### Collections

- `ProtobufField[]`
- `ProtobufFieldValue[]`

### Bridges

- `ProtobufWireParser`
- `ProtobufWireMaterializer`

## Read next

- [Parse a Protocol Buffers wire message](../how-to/parse-protobuf-wire-message.md)
- [Protocol Buffers package shape explanation](../explanation/protobuf-package-shape.md)
- [Protocol Buffers authoritative sources](protobuf-authoritative-sources.md)

## Source links

- [ProtobufWire.fs](/source/src/BinaryParsec.Protocols.Protobuf/ProtobufWire.fs)
- [ProtobufWireParser.fs](/source/src/BinaryParsec.Protocols.Protobuf/ProtobufWireParser.fs)
- [ProtobufWireMaterializer.fs](/source/src/BinaryParsec.Protocols.Protobuf/ProtobufWireMaterializer.fs)
