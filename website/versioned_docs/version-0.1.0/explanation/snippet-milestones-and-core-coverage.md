# Snippet Milestones And Core Coverage

The snippet ladder exists to keep `BinaryParsec` honest.

Each snippet milestone adds one small, realistic consumer outside the core and uses that consumer to justify one missing capability family. The goal is not to build miniature protocol packages for their own sake. The goal is to grow the contiguous parser surface only when a real binary layout needs it.

## Why The Snippet Ladder Exists

The repository moved into snippet-driven coverage work after the first protocol-quality surface was in place.

That order matters:

- Modbus RTU established one package-quality bar.
- Tiny snippets then forced the missing common reading paths into the core one family at a time.
- Documentation could stay close to each added capability instead of being written after the design had already drifted.

This keeps the core aligned with the repo bias:

- low-level binary cursor and primitive layer first
- thin composition layer on top
- real parsing needs before speculative abstraction

## What Each Snippet Justified

### PNG chunk iterator

The PNG slice justified bounded repetition and chunk iteration over zero-copy byte slices.

Core pressure:

- exact signature matching with `Contiguous.expectBytes`
- repeated bounded payload reads with `Contiguous.take`
- chunk walking that stops on a structural terminator

### CAN classic header

The CAN snippet justified compact packed-field reads where the binary layout is mostly flags and narrow integer fields rather than byte-aligned integers.

Core pressure:

- single-bit reads with `Contiguous.bit`
- multi-bit reads with `Contiguous.bits`
- mixed flag and field decoding without hidden re-alignment

### Protocol Buffers wire fields

The Protocol Buffers snippet justified varints, field tags, and length-delimited payload reads.

Core pressure:

- `Contiguous.varUInt64`
- `Contiguous.takeVarintPrefixed`
- parser loops that skip unknown fields while keeping required fields explicit

### DEFLATE block prelude

The DEFLATE snippet justified least-significant-bit-first reads for formats whose packed bit ordering differs from the default register-style interpretation.

Core pressure:

- `Contiguous.bitsLsbFirst`
- non-byte-aligned reads across byte boundaries
- making bit ordering explicit at the primitive level rather than burying it in format-specific helpers

### ELF header and program-header lookup

The ELF snippet justified wider fixed-width reads, explicit endian choice, and offset-based dependent layout parsing.

Core pressure:

- `Contiguous.u16be` and `Contiguous.u16le`
- `Contiguous.u32be` and `Contiguous.u32le`
- `Contiguous.u64be` and `Contiguous.u64le`
- `Contiguous.readAt` for absolute table lookups without mutating the current cursor

### Modbus TCP MBAP transport header

The Modbus TCP snippet justified layered transport framing over a shared payload parser.

Core pressure:

- validating transport-specific headers before payload parsing
- reusing the same payload parser behind more than one framing layer
- keeping transport concerns in the protocol layer instead of widening the core API

### MIDI event stream

The MIDI snippet justified state threaded across a byte stream.

Core pressure:

- recursive parser loops that carry prior status forward
- lookahead with `Contiguous.peekByte`
- making stateful stream behavior explicit in parser code rather than hidden in the runner

## What The Ladder Says About The Core

The current core surface is now broad enough to cover the common contiguous-input reading paths that real binary formats keep asking for:

- byte-aligned reads and zero-copy slices
- big-endian and little-endian fixed-width integers
- most-significant-bit-first and least-significant-bit-first packed fields
- varints and varint-prefixed payloads
- bounded iteration over repeated structures
- offset-based reads for dependent layouts
- parser-authored state for stream-style formats

That does not mean the core is finished. It means the next growth should come from fuller protocol and format packages rather than from inventing more isolated primitives.

## Documentation Rule

Each snippet milestone should leave three documentation traces in the repository:

- explanation material that says why the capability exists
- a how-to that shows how to apply it
- reference material that records the relevant core entry points

That rule keeps the repo teachable while the core is still being shaped by new binary workloads.
