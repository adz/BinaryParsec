# Core Reading Patterns Reference

This page records the core `BinaryParsec` helpers that were justified by the snippet milestones.

It is not generated API reference. It is a usage map for the contiguous parser surface.

## Running And Position

- `Contiguous.run`
  Runs a parser from `ParsePosition.origin` and returns `ParseResult<'T>`.
- `Contiguous.position`
  Returns the current `ParsePosition` without consuming input.
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
  Returns a zero-copy slice of the next `count` bytes.
- `Contiguous.expectBytes`
  Matches an exact byte sequence and returns its slice.
- `Contiguous.takeRemainingMinus`
  Returns all remaining bytes except a fixed trailing count.
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

Byte-oriented primitives still require byte alignment. Mixed byte and bit parsing should make alignment boundaries explicit in parser code.

## Varints And Length-Delimited Payloads

- `Contiguous.varUInt64`
  Reads one unsigned 64-bit varint.
- `Contiguous.takeVarintPrefixed`
  Reads a varint length prefix followed by that many bytes as a `ByteSlice`.

These helpers were added for Protocol Buffers wire-format style parsing, but they are also appropriate for other varint-length-prefixed binary layouts.

## Composition And Control Flow

- `Contiguous.result`
  Lifts a value into a parser without consuming input.
- `Contiguous.failAt`
  Builds an explicit parse failure at a chosen cursor.
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
- `Contiguous.parse`
  Computation-expression builder for parser composition.

Use `map2`, `mergeSources`, and `and!`-style fixed-shape composition when the layout shape is static. Use `bind` or `let!`-driven control flow when later reads depend on earlier field values.

## Offset-Based Reads

- `Contiguous.readAt`
  Runs a parser at an absolute byte offset and then restores the original cursor.

Use this for dependent layouts such as ELF tables. Keep the offset calculation and validation in the caller so the jump stays visible and format-specific.
