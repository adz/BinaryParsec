# Plan

## Current Direction

The project is now moving out of the snippet-only phase into package completion work.

The core has enough coverage pressure from real snippets that the next step is to turn the existing consumers into proper package tracks without weakening the core boundary. Work should now proceed package by package: first the existing Modbus and PNG projects, then additional promoted packages driven by the snippet results.

The repository baseline now assumes the .NET 10 SDK and the standard repo-root `artifacts/` output layout.

Documentation is being organized around the Divio split, with current design notes under `docs/explanation/`.

The active sequence is:

1. keep the core F#-first and free to use file-scoped modules and other F#-native organization choices
2. keep every C#-friendly surface in non-core package layers
3. pull authoritative specs and technical references into appropriate non-core locations before or alongside package expansion
4. complete packages in a controlled order: existing Modbus and PNG first, then additional promoted consumers
5. keep docs and tests moving in the same change as each package step

## Why This Order

- It preserves the core/package boundary now that the snippet ladder has done its job.
- It keeps C# concerns entirely out of the core so F# organization and ergonomics stay clean.
- It makes package work reviewable by tying implementation and tests back to authoritative source material.
- It prevents ad hoc package growth by turning each promotion into an explicit tracked step.
- It keeps parser flow readable by requiring clear separation between tokenization and later processing logic.

## Constraints

- Do not move C#-friendly APIs into the core.
- Keep protocol and format consumers outside the core project once they prove the boundary is warranted.
- Pull specs, RFCs, grammars, and similar references into an appropriate non-core project or docs area when they materially guide implementation.
- Use layout comments to show binary structure only where they materially improve readability; rely on self-explanatory code everywhere else.
- Keep tokenization logic and later processing logic visually separate in implementations.
- Keep build output in the repo-root `artifacts/` folder rather than project-local `bin/` and `obj/` paths.
- Flesh out docs in the same sequence as package work so the repo stays teachable.

## Package Promotion Order

- harden and broaden `BinaryParsec.Protocols.Modbus`
- expand `BinaryParsec.Protocols.Png` into a fuller format package
- evaluate `MIDI` as the remaining explicit follow-on package candidate
- promote that candidate only when its package scope is justified

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
- the next work is to evaluate MIDI for promotion from snippet coverage into a dedicated package

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

These snippets have already served their core-pressure role. They are now candidate package seeds rather than the main execution model.

## Update Rule

This file must stay current.

When the direction changes, when a major task is completed, or when the order of work changes, update this file in the same piece of work.
