# Plan

## Current Direction

The snippet-to-package promotion pass is complete, and the first DX pass has now restored a clean build plus consistent parser behavior across the promoted packages.

The active stage is now the docs overhaul: a Docusaurus site, user-intent navigation, curated API hubs, executable examples, and a versioned archive for the current docs line.

The repository baseline still assumes the .NET 10 SDK and the standard repo-root `artifacts/` output layout.

Documentation remains organized around the Divio split, with current design notes under `docs/explanation/`.

The active sequence is:

1. preserve the core as an F#-first contiguous backend that stays simple and fast for already-buffered inputs
2. close the remaining allocation regressions introduced during the additive DX pass without reopening broader API churn
3. finish hardening the docs site content on top of the standard Docusaurus build and versioned docs routing
4. keep the source-linked executable examples generation step reliable as docs evolve
5. rewrite the API landing pages as family hubs with clear member maps and follow-on detail pages
6. run the consistency pass across guides, integrations, examples, and reference pages so the prose reads like one manual

## Why This Order

- The parser surface is now much closer to the intended readability target, so the next risk is not naming confusion but losing performance confidence in the hot paths.
- A deliberate docs site can make the existing guide, how-to, reference, and explanation material feel like one coherent manual instead of a directory tree.
- Executable examples and source links will make the docs more trustworthy without widening the public API.
- Versioning the docs now keeps the current line and the archived line cleanly separated while the site structure is still small enough to manage.
- The remaining parser-architecture work should stay visible in the plan, but it does not need to block the docs site rollout.

## Constraints

- Do not move C#-friendly APIs into the core.
- Keep protocol and format consumers outside the core project once they prove the boundary is warranted.
- Pull specs, RFCs, grammars, and similar references into an appropriate non-core project or docs area when they materially guide implementation.
- Use layout comments to show binary structure only where they materially improve readability; rely on self-explanatory code everywhere else.
- Keep tokenization logic and later processing logic visually separate in implementations.
- Keep build output in the repo-root `artifacts/` folder rather than project-local `bin/` and `obj/` paths.
- Flesh out docs in the same sequence as the docs site structure so the repo stays teachable.
- Do not add a generalized streaming parser stack until one concrete consumer proves what must be shared and what must stay backend-specific.
- Prefer preserving optionality over squeezing one backend harder; avoid optimizations or abstractions that make later execution strategies harder to introduce.
- Prefer additive surface improvements first so existing consumers remain valid while the clearer style is proven out.
- Do not hide binary structure behind convenience helpers that erase where bytes come from, how many are consumed, or where failures should point.
- Do not let DX work harden contiguous-only assumptions into the semantic parser model.
- Treat slice-taking, span-backed zero-copy access, and absolute offset helpers as potentially backend-specific until proven otherwise.
- Prefer parser vocabulary that could survive multiple execution backends even when the first implementation still runs on contiguous input.
- Do not let the push for handled non-contiguous input weaken the contiguous backend's current ergonomics or hot-path expectations.
- Keep visual parser-layout XML comments compact and structural; they should show framing and offsets, not repeat prose already covered in module docs.

## Next Track

- finish the remaining allocation-focused cleanup in the computation-expression path and the few package parsers still under zero-allocation tests
- finish hardening the docs site content now that the standard Docusaurus scaffold and versioned archive build correctly
- keep the executable examples pipeline and source-link sync step part of the docs build path
- finish the curated family hubs for the API and integration pages
- run the docs build plus tests and verify the generated output is source-aware and navigable

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
- the current first-contact docs now explain the basic contiguous mental model, parser naming, and `ByteSlice`, but the API still carries avoidable ceremony
- the core now exposes an additive `BinaryParsec.Syntax` module plus `takeRemaining`, `fail`, `runExact`, `parseExactly`, and `parseRemaining` helpers to make parser intent clearer without breaking the existing contiguous surface
- the `BinaryParsec.Syntax` module is now explicitly classified into backend-neutral semantics versus contiguous-only conveniences to preserve future backend optionality
- representative Modbus TCP, Modbus PDU, Protocol Buffers, CAN classic, and ELF parsers now use the lower-ceremony style to prove it against framed, bounded, bit-packed, and offset-based shapes
- the tutorial, snippet how-to, and core reference now present the lower-ceremony style as the recommended parser-writing entry point
- the compiler and parser-behavior regressions uncovered during the DX sweep have been fixed, and the remaining known DX regression is confined to the zero-allocation hot-path tests for the computation-expression path
- the docs overhaul is now the active focus, with a standard Docusaurus build, intent buckets, executable example pages, and a generated `0.1.0` versioned archive in place
- the docs build and preview scripts now run through Docusaurus directly, without custom SSR require hooks or alias loaders
- the backend-seam classification remains documented and reflected in the core so the docs work does not accidentally canonize contiguous-only semantics

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
