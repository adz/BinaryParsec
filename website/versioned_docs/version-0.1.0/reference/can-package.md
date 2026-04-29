---
slug: /model/can
title: CAN package reference
sidebar_position: 3
---

# CAN package reference

`BinaryParsec.Protocols.Can` currently focuses on classic controller-frame tokenization plus a validated owned frame parser for base-format CAN frames.

## What this shows

The package reads the packed controller header as a structural slice first, then materializes the owned frame only when the caller asks for it.

## Core shape

- `CanClassic`
- `CanClassicFrameSlice`
- `CanClassicFrame`

The main boundary is between packed tokenization and owned-frame materialization.

## What you can do

- Parse a base-format CAN classic frame as a zero-copy slice.
- Materialize a stable owned frame for consumer code.
- Keep the raw payload boundary visible when the caller needs it.

## Member map

### Create and run

- `CanClassic.frame`
- `CanClassic.tryParseFrame`
- `CanClassic.parseFrame`

### Collections

- `CanClassicFrameSlice.Payload`
- `CanClassicFrame.Data`

### Runtime helpers

- `ParseResult<'T>`
- `ParseError`
- `ByteSlice`

### Bridges

- `CanClassicMaterializer`
- `CanClassicParser`

## Read next

- [Parse a CAN classic controller frame](../how-to/parse-can-classic-frame.md)
- [CAN package shape explanation](../explanation/can-package-shape.md)
- [CAN authoritative sources](can-authoritative-sources.md)

## Source links

- [CanClassic.fs](/source/src/BinaryParsec.Protocols.Can/CanClassic.fs)
- [CanClassicParser.fs](/source/src/BinaryParsec.Protocols.Can/CanClassicParser.fs)
- [CanClassicMaterializer.fs](/source/src/BinaryParsec.Protocols.Can/CanClassicMaterializer.fs)
