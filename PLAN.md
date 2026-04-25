# Plan

## Current Direction

The snippet-to-package promotion pass is now complete.

The next stage is not another backend implementation. The next stage is to reopen the architecture around execution backends without prematurely generalizing the parser surface.

The repository baseline still assumes the .NET 10 SDK and the standard repo-root `artifacts/` output layout.

Documentation remains organized around the Divio split, with current design notes under `docs/explanation/`.

The active sequence is:

1. preserve the core as an F#-first contiguous backend that stays simple and fast for already-buffered inputs
2. separate parser semantics from contiguous-only execution details before adding any incremental or streaming backend
3. add explicit guardrails against premature optimization and premature backend abstraction in plans, docs, and task sequencing
4. prove the needed backend seam with one real streaming-driven consumer before widening the core
5. keep docs and tests moving in the same change as each architecture step

## Why This Order

- It avoids a second premature move after the earlier contiguous-first move: jumping straight into a generalized backend abstraction without proving the seam.
- It keeps the proven contiguous path intact for files, captures, tests, and already-buffered messages.
- It addresses the real architectural problem directly: the current parser representation is too tightly fused to one execution strategy.
- It keeps review grounded in one concrete pressure case instead of broad speculative infrastructure.
- It preserves the core/package boundary and keeps C# concerns out of the core.

## Constraints

- Do not move C#-friendly APIs into the core.
- Keep protocol and format consumers outside the core project once they prove the boundary is warranted.
- Pull specs, RFCs, grammars, and similar references into an appropriate non-core project or docs area when they materially guide implementation.
- Use layout comments to show binary structure only where they materially improve readability; rely on self-explanatory code everywhere else.
- Keep tokenization logic and later processing logic visually separate in implementations.
- Keep build output in the repo-root `artifacts/` folder rather than project-local `bin/` and `obj/` paths.
- Flesh out docs in the same sequence as package work so the repo stays teachable.
- Do not add a generalized streaming parser stack until one concrete consumer proves what must be shared and what must stay backend-specific.
- Prefer preserving optionality over squeezing one backend harder; avoid optimizations or abstractions that make later execution strategies harder to introduce.

## Next Architecture Track

- document the parser-semantics versus backend-mechanics split explicitly
- choose one real streaming pressure case, most likely Modbus RTU over serial input
- design the minimum backend seam needed to avoid switching grammar-level parsers
- only then decide whether the seam is a new parser representation, a runner abstraction, or a narrower package-level split
- refuse backend work that cannot name both the shared parser semantics and the intentionally backend-specific mechanics
- prefer a package-edge serial framing seam first when one concrete consumer can be solved without widening the core

## Current Status

- the core is now a contiguous `ReadOnlySpan<byte>` runner over explicit `ParsePosition`, with byte, endian, slice, and bit primitives
- the composition layer is in place with `map`, `bind`, sequencing helpers, and a computation-expression entry point
- the PNG and Modbus RTU slices now live in separate `BinaryParsec.Protocols.*` projects and are validated with offset-aware diagnostics
- the PNG slice now covers signature matching, one-chunk reads, and chunk iteration through `IEND` with zero-copy chunk envelopes
- `BinaryParsec.Protocols.Modbus` now exposes package-quality RTU parse entry points, stable owned frame models, and C#-friendly overloads
- `BinaryParsec.Protocols.Modbus` now tokenizes RTU and TCP transport frames separately, reuses one shared Modbus PDU layer, and exposes stable RTU/TCP facades
- `BinaryParsec.Protocols.Can` now exposes classic controller-frame tokenization, authoritative-source-backed package docs, and a validated owned frame facade for base-format classic CAN
- the public surface now carries the needed purpose-and-fit comments and concise API docs
- Modbus package docs now cover RTU and TCP usage, package reference material, the stable-facade versus low-level-parser split, and the authoritative Modbus source documents that drive the package
- CAN package docs now cover the classic controller-frame scope, the split between packed tokenization and owned-model materialization, and the authoritative CAN sources that drive the package
- the snippet ladder is now documented with capability rationale, a core snippet how-to, and a handwritten reference map for the relevant contiguous parser helpers
- the repo direction now explicitly treats the C# layer as independent from the core so the core can stay F#-first and use file-scoped modules freely
- package expansion now requires authoritative source material to live outside the core and to guide implementation, docs, and tests
- implementation guidance now explicitly prefers layout comments only where binary structure benefits from them and expects tokenization and processing logic to stay visually distinct
- builds stage assemblies and XML docs under `artifacts/api-docs/` for generated reference consumption
- successful hot paths for the primitive, PNG, and Modbus RTU slices stay allocation-free
- fixed-shape core composition now has an allocation-free path through direct applicative combinators and `and!` computation-expression lowering
- the PNG initial slice and Modbus RTU fixed header now use the cleaner fixed-shape composition path without regressing hot-path allocation behavior
- the protocol-layer C# direction is confirmed as thin `BinaryParsec.Protocols.*` facades over the F#-first core
- test coverage now runs through `dotnet test` in `BinaryParsec.Tests` with Unquote-backed assertions
- a tiny CAN classic controller-style header snippet now covers base-identifier extraction, packed control flags, and CAN classic DLC validation
- a tiny Protocol Buffers wire-field snippet now covers varints, field tags, length-delimited payloads, and unknown-field skipping
- a tiny DEFLATE block-prelude snippet now covers least-significant-bit-first packed fields, arbitrary-width extraction, and non-byte-aligned reads
- a tiny ELF header-plus-program-header snippet now covers 32-bit and 64-bit width reads, little-endian and big-endian selection, and absolute offset-based table lookups
- a tiny Modbus TCP MBAP snippet now covers MBAP transport validation and shared Modbus PDU parsing over a distinct transport frame
- a tiny MIDI channel-event stream snippet now covers delta-time VLQs, running status reuse, and state threaded across byte-stream events
- `BinaryParsec.Protocols.Png` now exposes package-quality chunk tokenization, validated static-PNG file parsing, and package docs tied to the current W3C PNG specification
- `BinaryParsec.Protocols.Protobuf` now exposes package-quality Protocol Buffers wire-field tokenization, a thin owned field collector, and package docs tied to the official protobuf wire-format references
- `BinaryParsec.Protocols.Deflate` now exposes package-quality DEFLATE block-header and dynamic-prelude tokenization, with dynamic-Huffman counts kept separate from later Huffman decoding and package docs tied to RFC 1951
- `BinaryParsec.Protocols.Elf` now exposes package-quality ELF header tokenization, indexed program-header lookup, and package docs tied to the generic ELF ABI header and program-header definitions
- `BinaryParsec.Protocols.Midi` now exposes package-quality channel-event parsing with delta-time VLQs, running status, a narrow owned event model, and docs that keep package scope intentionally small
- the backend-seam guardrail is now documented explicitly so future streaming work must justify what is shared and what remains backend-specific
- the first concrete seam design now targets Modbus RTU over serial input and prefers package-edge frame accumulation ahead of any generalized incremental core
- the next work is to address the parser/backend seam so real streaming consumers do not imply switching format grammars

## Completed Promotion Pass

The snippet ladder has now been promoted into package work where justified:

1. PNG chunk iterator
   Promoted into `BinaryParsec.Protocols.Png`.
2. CAN classic frame header snippet
   Promoted into `BinaryParsec.Protocols.Can`.
3. Protocol Buffers wire-field snippet
   Promoted into `BinaryParsec.Protocols.Protobuf`.
4. DEFLATE block prelude snippet
   Promoted into `BinaryParsec.Protocols.Deflate`.
5. ELF header and table-entry snippet
   Promoted into `BinaryParsec.Protocols.Elf`.
6. Modbus TCP MBAP snippet
   Folded into the broader `BinaryParsec.Protocols.Modbus` package.
7. MIDI event snippet
   Promoted into `BinaryParsec.Protocols.Midi`.

## Update Rule

This file must stay current.

When the direction changes, when a major task is completed, or when the order of work changes, update this file in the same piece of work.
