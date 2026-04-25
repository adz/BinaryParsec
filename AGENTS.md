# AGENTS.md

Repo-specific instructions for agents working in `BinaryParsec`.

User instructions override this file when they explicitly conflict.

## Purpose

`BinaryParsec` exists to provide an idiomatic F# library for high-performance binary tokenization and parsing.

Project priorities:

- developer experience for humans and LLMs
- intuitive and consistent API design
- high performance for realistic binary parsing workloads
- architecture driven by real parsing requirements
- strong documentation and clear implementation rationale

## Core Direction

Preserve these defaults unless the user overrides them:

- Prefer a dual-layer design.
  Build a low-level binary cursor and primitive layer first, then a thin combinator and computation-expression layer on top.
- Keep the core F#-idiomatic.
  `BinaryParsec` itself should feel natural in F# first.
- Keep the C# layer independent of the core.
  Core projects may use file-scoped modules and other F#-first organization choices freely. C#-friendly surfaces belong in `BinaryParsec.Protocols.*` or other non-core consumer packages, not in the core API shape.
- Treat real parsing needs as the driver of architecture.
  Do not add advanced features, speculative abstractions, or extension points until concrete parsing scenarios justify them.

## Design Rules

- Optimize for consistency and DX before cleverness.
- Keep abstractions at the same level of concern.
- Avoid local optimization that harms wider system design.
- Aim for APIs that feel obvious to use and hard to misuse.
- Keep public APIs small until real usage forces expansion.
- Prefer simple composition over deep abstraction stacks.
- Pull repeated logic out when the reuse is real and the abstraction stays clear.
- Do not introduce boilerplate-heavy structures or hypothetical infrastructure.
- Prefer one public module, type, or class per file.
- Only keep multiple public types in one file when they form a very small, tightly related family.
- When compactness and navigability conflict, prefer navigability.
- Prefer computation expressions for parser composition above the primitive layer.
- Fall back to manual parser chaining only when a measured hot path or clearer error/position control justifies it.

## Parser Architecture

The intended direction is:

- a low-level binary input model
- binary primitives for bytes, bits, endianness, framing, slices, and validation
- a thin parser/combinator layer above those primitives
- separate execution backends where justified, especially contiguous span-based and streaming/buffered input

Do not:

- build two separate public parser libraries for span and stream
- let `ref struct` concerns leak through the whole design without need
- force backend-specific capabilities into a fake common denominator

Backend-specific behavior is acceptable where reality requires it.

## Protocol And Format Packages

`BinaryParsec.Protocols.*` and other non-core consumer packages are first-class consumers of the core.

Rules:

- Prefer straightforward parse entry points for consumers.
- Expose C#-friendly APIs only at the package layer where useful.
- Keep packages thin over the core parser engine.
- Separate transport framing from shared payload parsing when the format supports it.
- Keep specs, RFCs, grammars, and other authoritative technical references in the relevant non-core project or docs area, not in the core project.
- Use real protocols and binary formats to pressure the design.

Near-term direction:

- keep expanding `Modbus`
- grow `PNG` into a fuller non-protocol package example
- develop `CAN`

## Spec-Driven Implementation

Where a protocol or format has a published specification, RFC, grammar, BNF, or similarly authoritative description:

- implementation should be guided by that source
- docs and tests should reference the source when it clarifies behavior
- subtle or disputed behavior should be verified against the source rather than inferred casually
- the source material should be pulled into the repository structure in an appropriate non-core location when it materially helps implementation or review

When useful, add validation against formal definitions through tests, including:

- property generators driven by grammar or spec constraints
- protocol fixtures derived from or checked against the spec
- invariant checks that prove parser behavior matches documented structure

## Implementation Style

- Write code so the tokenization path and later processing path stay visually distinct.
- Use compact layout comments where they materially improve readability of binary structure, offsets, or parsing flow.
- Comments should explain what is going on when it is not immediately obvious.
- The code should still strive to be self-explanatory; do not narrate obvious lines.

## Testing

Testing is required and should be layered intentionally.

Priority order:

1. feature-level integration tests
2. targeted unit tests
3. property tests where they add real confidence
4. protocol/format stress tests
5. benchmarks

Additional rules:

- start from feature behavior, then expand into narrower tests as needed
- prefer property tests where they meaningfully simplify or replace narrower tests
- remove or simplify obsolete tests when stronger tests make them redundant
- use protocol and format examples to stress semantics, not just happy paths
- consider mutation testing where it adds value
- use snapshot testing where structure or diagnostics benefit from stable review

## Documentation

Documentation quality is a project requirement.

Agents must:

- write practical, plain, technically clear docs
- avoid generic conversational filler
- avoid repo prose that depends on external conversation state
- write documentation that stands on its own inside the repository

Follow the Divio split:

- tutorials for guided learning
- how-to guides for task completion
- reference docs for factual API/material
- explanation docs for design reasoning

Public-facing modules and types should usually explain:

- what they are for
- why they exist
- how they fit with the rest of the system when that is not obvious

API doc comments should be concise and useful. Do not add them mechanically when the signature is already clear.

## Plan And Task Tracking

- `PLAN.md` is the current project plan and must stay up to date.
- `TASKS.md` is the execution checklist and must stay explicit, numbered, and checkable.
- Update `PLAN.md` and `TASKS.md` in the same change when direction or execution order materially changes.

## Change Control

Ask before:

- adding dependencies
- changing target frameworks
- introducing unsafe code
- adding benchmark projects or benchmark infrastructure
- changing established public API shape in a substantial way
- creating extra projects

When in doubt, ask before widening scope.

## Decision Standard

When several approaches are possible, prefer the one that best preserves:

1. consistency
2. developer experience
3. architectural clarity
4. performance in realistic binary workloads
5. minimal necessary abstraction

If an approach improves one small area while making the system less coherent, do not choose it without strong evidence.
