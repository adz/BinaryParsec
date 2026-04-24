# Plan

## Current Direction

The project is now moving from foundation work into coverage work.

The immediate goal is to cover the common binary reading paths by driving the core from tiny, real snippet parsers taken from real protocols and binary formats. Each snippet should justify one missing capability with the smallest realistic consumer. Once the core is mostly fleshed out, the project should return to full protocol and format packages and complete them to production quality.

The repository baseline now assumes the .NET 10 SDK and the standard repo-root `artifacts/` output layout.

Documentation is being organized around the Divio split, with current design notes under `docs/explanation/`.

The active sequence is:

1. keep one production-ready protocol package in place as the quality bar
2. add tiny snippet consumers that each force a missing reading path into the core
3. document each newly justified capability in the same change
4. return to full protocol and format packages once the common reading paths are mostly covered

## Why This Order

- It keeps the core grounded in real binary parsing needs.
- It lets the library add exactly the reading helpers that real formats require.
- It keeps snippet work small enough to isolate one capability at a time.
- It establishes one package-quality protocol surface before multiplying protocol coverage.
- It prevents half-finished full protocol packages from driving premature API growth.
- It keeps C# concerns at the protocol layer rather than distorting the core too early.

## Constraints

- Do not add a core primitive until a real snippet clearly needs it.
- Do not jump to full protocol completion when a smaller snippet can justify the next missing capability.
- Keep protocol and format consumers outside the core project once they prove the boundary is warranted.
- Do not add C#-specific compromises to the core before a protocol package needs them.
- Keep build output in the repo-root `artifacts/` folder rather than project-local `bin/` and `obj/` paths.
- Flesh out docs in the same sequence as the capability work so the repo stays teachable.

## Coverage Targets

- fixed-width integer reads across common widths and endianness
- multi-bit and packed-flag reads
- bounded repetition and chunk iteration
- varints and length-delimited payloads
- offset-based reads and dependent layout parsing
- backend-aware parsing guidance for eventual incremental input

## Current Status

- the core is now a contiguous `ReadOnlySpan<byte>` runner over explicit `ParsePosition`, with byte, endian, slice, and bit primitives
- the composition layer is in place with `map`, `bind`, sequencing helpers, and a computation-expression entry point
- the PNG and Modbus RTU slices now live in separate `BinaryParsec.Protocols.*` projects and are validated with offset-aware diagnostics
- the PNG slice now covers signature matching, one-chunk reads, and chunk iteration through `IEND` with zero-copy chunk envelopes
- `BinaryParsec.Protocols.Modbus` now exposes package-quality RTU parse entry points, stable owned frame models, and C#-friendly overloads
- the public surface now carries the needed purpose-and-fit comments and concise API docs
- Modbus package docs now cover RTU usage, package reference material, and the stable-facade versus low-level-parser split
- builds stage assemblies and XML docs under `artifacts/api-docs/` for generated reference consumption
- successful hot paths for the primitive, PNG, and Modbus RTU slices stay allocation-free
- fixed-shape core composition now has an allocation-free path through direct applicative combinators and `and!` computation-expression lowering
- the PNG initial slice and Modbus RTU fixed header now use the cleaner fixed-shape composition path without regressing hot-path allocation behavior
- the protocol-layer C# direction is confirmed as thin `BinaryParsec.Protocols.*` facades over the F#-first core
- test coverage now runs through `dotnet test` in `BinaryParsec.Tests` with Unquote-backed assertions
- a tiny CAN classic controller-style header snippet now covers base-identifier extraction, packed control flags, and CAN classic DLC validation
- a tiny Protocol Buffers wire-field snippet now covers varints, field tags, length-delimited payloads, and unknown-field skipping
- a tiny DEFLATE block-prelude snippet now covers least-significant-bit-first packed fields, arbitrary-width extraction, and non-byte-aligned reads
- the next task is to cover width/endian completeness and offset-based reads with a tiny ELF snippet

## Snippet Ladder

Use the smallest realistic snippet that proves each missing reading pattern:

1. PNG chunk iterator
   Drives repeated bounded reads, chunk iteration, and reusable magic matching.
2. CAN classic frame header snippet
   Drives multi-bit extraction, packed flags, and compact frame metadata.
3. Protocol Buffers wire-field snippet
   Drives varints, field tags, length-delimited payloads, and unknown-field skipping.
4. DEFLATE block prelude snippet
   Drives arbitrary-width bit extraction and bit-order correctness.
5. ELF header and table-entry snippet
   Drives width/endian coverage, offset-based reads, and dependent layout parsing.
6. Modbus TCP MBAP snippet
   Drives layered transport framing over shared payload parsing.
7. MIDI event snippet
   Drives stateful byte-stream parsing and informs later incremental-input design.

## Update Rule

This file must stay current.

When the direction changes, when a major task is completed, or when the order of work changes, update this file in the same piece of work.
