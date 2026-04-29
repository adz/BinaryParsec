---
slug: /model/core
title: Core reading patterns
sidebar_position: 2
---

# Core reading patterns

This page records the core `BinaryParsec` helpers that were justified by the snippet milestones.

It is not generated API reference. It is a usage map for the contiguous parser surface.

If you are new to the library, start with [Parse Your First Sized Message](../tutorials/parse-your-first-sized-message.md) before using this page as a checklist.

## Mental Model

- A `ContiguousParser<'T>` is a parser value that knows how to read a `'T` from contiguous bytes.
- Parser bindings such as `let message = ...` usually name the thing being parsed, not an already-parsed result.
- `Contiguous.run` executes the parser against an input span.
- `ByteSlice` is a zero-copy `(offset, length)` descriptor into the original input, not a copied payload array.
- `open BinaryParsec.Syntax` is the recommended parser-writing style when you want the binary layout to stay visually dominant.

The most common control-flow pattern is:

- read one field
- use that field to decide the next read

For example, a 4-byte size prefix followed by that many payload bytes:

```fsharp
open BinaryParsec.Syntax

let message =
    parse {
        let! size = u32be
        let! payload = takeSlice (int size)
        return size, payload
    }
```

When the payload should be parsed immediately rather than kept as a slice, prefer `parseExactly`:

```fsharp
open BinaryParsec.Syntax

let payload =
    parse {
        let! command = ``byte``
        let! argument = u16be
        return command, argument
    }

let message =
    parse {
        let! payloadLength = ``byte``
        let! parsedPayload = parseExactly (int payloadLength) payload
        return payloadLength, parsedPayload
    }
```

## Running And Position

- `Contiguous.run`
  Runs a parser from `ParsePosition.origin` and returns `ParseResult<'T>`.
- `Contiguous.runWith`
  Runs a parser from an explicit starting position.
- `Contiguous.position`
  Returns the current `ParsePosition` without consuming input.
- `Contiguous.requireByteAligned`
  Fails if the current cursor is not at a byte boundary.
- `Contiguous.remainingBytes`
  Returns the number of unread bytes from the current cursor.
- `ParsePosition.origin`
  The byte `0`, bit `0` starting position.
- `ParsePosition.create`
  Creates a validated explicit position.

Use these when parser control flow or diagnostics depend on the current cursor.

## Byte-Aligned Reads And Slices

- `Contiguous.byte`
  Reads one byte.
- `Contiguous.peekByte`
  Reads one byte without advancing.
- `Contiguous.skip`
  Advances by a byte count.
- `Contiguous.take`
  Returns a zero-copy slice of the next `count` bytes (aliased as `takeSlice` in `Syntax`).
- `Contiguous.expectBytes`
  Matches an exact byte sequence and returns its slice.
- `Contiguous.takeRemainingMinus`
  Returns all remaining bytes except a fixed trailing count.
- `Contiguous.takeRemaining`
  Returns all remaining unread bytes as one slice.
- `ByteSlice.create`
  Creates a validated zero-copy slice descriptor.
- `ByteSlice.asSpan`
  Resolves a `ByteSlice` back to a `ReadOnlySpan<byte>`.

These helpers cover fixed signatures, bounded payload reads, framing bytes, and zero-copy payload exposure.

## Fixed-Width Integer Reads

- `Contiguous.u16be`
- `Contiguous.u16le`
- `Contiguous.u32be`
- `Contiguous.u32le`
- `Contiguous.u64be`
- `Contiguous.u64le`

Use the endian-specific primitive that matches the format definition directly. Do not read raw bytes and reassemble them manually unless a measured hot path demands it.

## Packed Bit Reads

- `Contiguous.bit`
  Reads one most-significant-bit-first flag.
- `Contiguous.bits`
  Reads `1` to `32` bits in most-significant-bit-first order.
- `Contiguous.bitsLsbFirst`
  Reads `1` to `32` bits in least-significant-bit-first order.

Use `bit` and `bits` for register-style layouts such as controller headers. Use `bitsLsbFirst` for formats such as DEFLATE that define low-bit-first extraction.

Byte-oriented primitives still require byte alignment. Mixed byte and bit parsing should make alignment boundaries explicit in parser code using `requireByteAligned`.

## Varints And Length-Delimited Payloads

- `Contiguous.varUInt64`
  Reads one unsigned 64-bit varint.
- `Contiguous.takeVarintPrefixed`
  Reads a varint length prefix followed by that many bytes as a `ByteSlice` (aliased as `takeVarintSlice` in `Syntax`).

These helpers were added for Protocol Buffers wire-format style parsing, but they are also appropriate for other varint-length-prefixed binary layouts.

## Composition And Control Flow

- `Contiguous.result`
  Lifts a value into a parser without consuming input.
- `Contiguous.failAt`
  Builds an explicit parse failure at a chosen cursor.
- `Contiguous.fail`
  Builds a failing parser at a chosen cursor.
- `Contiguous.label`
  Attaches a label to a parser to improve error messages (aliased as `<?>` in `Syntax`).
- `Contiguous.map`
  Maps a parser result without changing consumption.
- `Contiguous.map2`
  Combines two fixed-shape parsers without intermediate allocations.
- `Contiguous.mergeSources`
  Returns a struct tuple from two fixed-shape parsers.
- `Contiguous.bind`
  Chooses the next parser from the previous value.
- `Contiguous.zip`
  Sequences two parsers into a normal tuple.
- `Contiguous.keepLeft`
  Runs two parsers and keeps the left value.
- `Contiguous.keepRight`
  Runs two parsers and keeps the right value.
- `Contiguous.choice`
  Chooses between multiple parsers (aliased as `<|>` in `Syntax`).
- `Contiguous.optional`
  Attempts to run a parser, returning `None` if it fails.
- `Contiguous.many` / `Contiguous.many1`
  Runs a parser zero/one or more times until it fails.
- `Contiguous.sepBy` / `Contiguous.sepBy1`
  Parses a sequence of items separated by a separator.
- `Contiguous.between`
  Runs a parser surrounded by open and close parsers.
- `Contiguous.parse`
  Computation-expression builder for parser composition.
- `Contiguous.parseExactly`
  Parses exactly the next bounded byte range as a nested payload.
- `Contiguous.parseRemaining`
  Parses all remaining unread bytes as one bounded nested payload.

## Syntax Operators

When `BinaryParsec.Syntax` is open, the following FParsec-style operators are available:

- `>>=` : `bind`
- `<!>` : `map`
- `|>>` : `map` (piped)
- `.>>.` : `zip`
- `.>>` : `keepLeft`
- `>>.` : `keepRight`
- `<|>` : `choice` between two parsers
- `<?>` : `label`

## Offset-Based Reads

- `Contiguous.readAt`
  Runs a parser at an absolute byte offset and then restores the original cursor.

Use this for dependent layouts such as ELF tables. Keep the offset calculation and validation in the caller so the jump stays visible and format-specific.

## Read next

- [Parse your first sized message](../tutorials/parse-your-first-sized-message.md)
- [Build a snippet parser](../how-to/build-a-snippet-parser.md)
- [API hubs](./api/README.md)
