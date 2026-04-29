---
slug: /model/png
title: PNG package reference
sidebar_position: 7
---

# PNG package reference

`BinaryParsec.Protocols.Png` exposes zero-copy chunk tokenizers and a validated file-level parser.

## What this shows

The PNG package keeps the signature, chunk envelope, chunk stream, and owned file model separate. That makes the binary structure visible while still giving callers a clean file-level entry point.

## Core shape

- `Png`
- `PngChunkEnvelope`
- `PngSlice`
- `PngChunkStream`
- `PngImageHeader`
- `PngChunk`
- `PngFile`

## What you can do

- Match the PNG signature and read the first chunk without copying.
- Walk chunk envelopes through `IEND`.
- Materialize a validated PNG file model from a contiguous datastream.

## Member map

### Create and run

- `Png.signature`
- `Png.chunkEnvelope`
- `Png.initialSlice`
- `Png.chunkStream`
- `Png.file`
- `Png.tryParseFile`
- `Png.parseFile`

### Collections

- `PngChunkStream.Chunks`
- `PngFile.Chunks`

### Bridges

- `PngParser`
- `PngMaterializer`

## Read next

- [Parse a PNG file](../how-to/parse-png-file.md)
- [PNG package shape explanation](../explanation/png-package-shape.md)
- [PNG authoritative sources](png-authoritative-sources.md)

## Source links

- [Png.fs](/source/src/BinaryParsec.Protocols.Png/Png.fs)
- [PngParser.fs](/source/src/BinaryParsec.Protocols.Png/PngParser.fs)
- [PngMaterializer.fs](/source/src/BinaryParsec.Protocols.Png/PngMaterializer.fs)
