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

Do not let the first backend harden into the parser abstraction.

The current contiguous `ReadOnlySpan<byte>` runner is a useful and deliberate first backend, but it should stay a backend choice rather than a commitment that every future consumer must share the same execution model. The next architecture step should therefore be to identify the minimum seam between parser semantics and backend mechanics before adding any generalized streaming or incremental infrastructure.

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

## Capability Pressure

The next stage should use tiny, real snippet parsers to justify the missing common binary reading paths one by one.

The preferred sequence is:

1. PNG chunk iterator
2. CAN classic frame header snippet
3. Protocol Buffers wire-field snippet
4. DEFLATE block-prelude snippet
5. ELF header and table-entry snippet
6. Modbus TCP MBAP snippet
7. MIDI event snippet

After those snippets cover the common paths, the project should return to broader package completion with a more capable core.
