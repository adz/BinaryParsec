---
slug: /model/deflate
title: DEFLATE package reference
sidebar_position: 4
---

# DEFLATE package reference

`BinaryParsec.Protocols.Deflate` exposes the packed DEFLATE block-header layer and the dynamic-Huffman count prelude over the contiguous core.

## What this shows

The package intentionally stops at the structural prelude. It reads the packed block header, the dynamic count metadata, and leaves Huffman-table construction to later code.

## Core shape

- `Deflate`
- `DeflateBlockHeader`
- `DeflateDynamicPrelude`
- `DeflateBlockType`

## What you can do

- Read `BFINAL` and `BTYPE` from the packed least-significant-bit-first stream.
- Read the dynamic-prelude counts needed for later tree construction.
- Keep later decoding logic separate from the tokenization step.

## Member map

### Create and run

- `Deflate.blockHeader`
- `Deflate.dynamicPrelude`

### Runtime helpers

- `bitsLsbFirst`
- `parse`
- `requireByteAligned`

### Bridges

- `DeflateParser`

## Read next

- [Parse a DEFLATE dynamic block prelude](../how-to/parse-deflate-dynamic-prelude.md)
- [DEFLATE package shape explanation](../explanation/deflate-package-shape.md)
- [DEFLATE authoritative sources](deflate-authoritative-sources.md)

## Source links

- [Deflate.fs](/source/src/BinaryParsec.Protocols.Deflate/Deflate.fs)
- [DeflateParser.fs](/source/src/BinaryParsec.Protocols.Deflate/DeflateParser.fs)
- [DeflateDynamicPrelude.fs](/source/src/BinaryParsec.Protocols.Deflate/DeflateDynamicPrelude.fs)
