# Architecture Notes

## Product Shape

The library should be layered:

- `BinaryParsec`
  Core binary input model, primitives, thin combinators, CE support, and diagnostics.
- `BinaryParsec.Protocols.*`
  Protocol-specific libraries that expose ready-to-use parsers and domain models.
- `docs/` and fixture folders
  Design notes, examples, and stress cases that drive API design and performance decisions.

## Core Design Bias

Take inspiration from `FParsec` at the API level, not as a compatibility target or implementation model.

The project should favor a dual-layer design:

- low-level binary cursor and primitive operations first
- thin parser/combinator and computation-expression support on top

This keeps the real binary mechanics visible while still allowing an ergonomic F# surface.

Useful concepts to borrow:

- parser values
- sequencing and choice
- labels and expected-input diagnostics
- explicit backtracking control
- optional user state

Things that must be binary-first rather than text-first:

- fixed-width integer reads
- endianness-aware primitives
- bitfield extraction
- bounded payload parsers
- zero-copy slices where safe
- incremental input support
- explicit framing and offset handling

Agents should resist speculative abstraction here. The architecture should move when real formats and protocols demand it.

## Backend Shape

Do not build two separate public parser libraries. Build one semantic model with at least two execution backends:

- span-backed contiguous input
- stream-backed or buffered incremental input

The parser vocabulary, result model, and most combinators should stay aligned across both. The low-level cursor and execution path can differ.

Contiguous span-backed input is the first implementation target. Incremental input should follow only after the contiguous core is proven by real consumers.

Some APIs should remain backend-specific. For example:

- returning a borrowed span is natural for contiguous input
- a stream backend may need to return copied or owned data instead

The core abstraction should not erase meaningful differences just to keep the types symmetrical.

## C# Usability

The core F# CE-centric surface can stay F#-idiomatic. Protocol packages should add plain .NET entry points that are straightforward from C#:

- static parse methods
- simple option/result wrappers or exceptions where appropriate
- POCO-like result types or readonly records/classes
- no requirement to use a computation expression from C#

The core API should not be bent away from idiomatic F# just to make every layer equally convenient from C#.

## Spec-Driven Work

Where authoritative specs exist, they should shape implementation and verification.

This does not require citation noise everywhere, but it does require:

- checking subtle behavior against the actual source
- expressing important protocol and format rules in tests
- using property tests or invariants where a grammar or structured definition makes that practical

## First Pressure Tests

The initial library shape should be tested by:

- one non-protocol binary format with nested chunks
- one industrial protocol with checksums
- one bit-heavy frame format
- one varint-driven format after the first protocol slices sharpen the core

The current preferred sequence is:

1. PNG
2. Modbus RTU
3. CAN
4. Protocol Buffers wire parsing
5. Modbus TCP
