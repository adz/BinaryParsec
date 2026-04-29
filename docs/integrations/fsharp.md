---
title: F# callers
sidebar_position: 3
---

# F# callers

BinaryParsec is at its clearest when F# code uses the contiguous parser layer directly.

## What this shows

The F# surface keeps the binary layout visible. `BinaryParsec.Syntax` gives you a compact parser-writing style, and the primitive helpers stay close to bytes, bits, slices, and offsets.

## What can you do

- Build parsers with `parse`, `let!`, `and!`, `map`, and `bind`.
- Use `takeSlice`, `parseExactly`, and `readAt` when the binary structure needs to stay explicit.
- Thread offsets and parse errors without dropping down to a separate abstraction layer.

## Core shape

The core parser value is `ContiguousParser<'T>`. That keeps the first-contact experience compact and the package implementations close to the binary format they are reading.

## Read next

- [Core reading patterns](../reference/core-reading-patterns.md)
- [Parse your first sized message](../tutorials/parse-your-first-sized-message.md)
- [Build a snippet parser](../how-to/build-a-snippet-parser.md)

