# AGENTS.md

This file defines strict repo-specific instructions for agents working in `BinaryParsec`.

User instructions override this file when they explicitly conflict.

## Purpose

`BinaryParsec` exists to provide an idiomatic F# library for high-performance binary tokenization and parsing.

The project priorities are:

- developer experience for humans and LLMs
- intuitive and consistent API design
- high performance suitable for binary parsing workloads
- architecture driven by real parsing requirements
- strong documentation and clear implementation rationale

## Core Bias

Agents must preserve these defaults unless the user overrides them:

- Prefer a dual-layer design.
  Build a low-level binary cursor and primitive layer first, then a thin combinator and computation-expression layer on top.
- Keep the core F#-idiomatic.
  `BinaryParsec` itself should feel natural in F# first.
- Keep C# friendliness focused on `BinaryParsec.Protocols.*`.
  Do not distort the core API to optimize for C# if that harms F# ergonomics.
- Treat real parsing needs as the driver of architecture.
  Do not add advanced features, speculative abstractions, or extension points until they are justified by concrete parsing scenarios.

## Design Rules

Agents must follow these design rules:

- Optimize for consistency and DX before cleverness.
- Keep abstractions at the same level of concern.
- Avoid local optimization that harms the wider system design.
- Prefer explicit behavior, but not at the expense of a clean and intuitive API.
- Aim for APIs that feel obvious to use and hard to misuse.
- Avoid hidden edge cases in the public surface.
- Keep public APIs small until real usage forces expansion.
- Prefer simple composition over deep abstraction stacks.
- Pull repeated logic out when the reuse is real and the abstraction stays clear.
- Do not introduce boilerplate-heavy code structures.
- Do not add infrastructure that exists only for hypothetical future needs.
- Prefer one public module, type, or class per file.
- Only keep multiple public types in one file when they form a very small, tightly related family and splitting them would be clearly worse for readability.
- When compactness and navigability conflict, prefer navigability.

## Anti-Patterns

Agents should avoid:

- boilerplate-heavy implementations
- overly abstract designs
- repeated code that should clearly be factored out
- premature optimization at the wrong layer
- public API growth without concrete parsing pressure
- implementation choices driven by novelty rather than fit
- documentation written in generic LLM style

## Parser Architecture

The intended architectural direction is:

- a low-level binary input model
- binary primitives for bytes, bits, endianness, framing, slices, and validation
- a thin parser/combinator layer above those primitives
- separate execution backends where justified, especially contiguous span-based and streaming/buffered input

Agents should not:

- build two separate public parser libraries for span and stream
- let `ref struct` concerns leak through the entire design without need
- force backend-specific capabilities into a fake common denominator

Backend-specific behavior is acceptable where reality requires it.

## Protocol Packages

`BinaryParsec.Protocols.*` should be treated as first-class consumers of the core.

Rules for protocol packages:

- Prefer straightforward parse entry points for consumers.
- Expose C#-friendly APIs at the protocol layer where useful.
- Keep protocol packages thin over the core parser engine.
- Separate transport framing from shared protocol payload parsing when the protocol structure supports it.
- Use real protocols and real binary formats to pressure the design.

Near-term direction:

- develop `Modbus`
- develop `CAN`
- develop one non-protocol format/package-level example

## Spec-Driven Implementation

Where a protocol or format has a published specification, RFC, grammar, BNF, or similarly authoritative description:

- implementations should be guided by that source
- docs and tests should reference the relevant source when it helps explain behavior
- subtle, constrained, or disputed behaviors should be verified against the spec rather than inferred casually

This rule is balanced rather than bureaucratic:

- not every trivial behavior needs an inline citation
- spec-backed behavior must still be reflected clearly in code and tests

When possible, agents should add validation against formal definitions through tests, including:

- property generators driven by grammar/spec constraints
- protocol fixtures derived from or checked against the spec
- invariant checks that prove parser behavior matches documented structure

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
- prefer property tests wherever they meaningfully simplify or replace narrower tests
- remove or simplify obsolete tests when stronger tests make them redundant
- use protocol and format examples to stress semantics, not just happy paths
- consider mutation testing where it adds value
- use snapshot testing where structure or diagnostics benefit from stable review

## Documentation

Documentation quality is a project requirement.

Agents must:

- write practical, plain, technically clear docs
- avoid generic conversational filler
- avoid repo prose such as `As discussed`, `the old way`, `previously`, or similar references to non-repo conversation state
- write documentation that stands on its own inside the repository

The repo should follow the Divio documentation system:

- tutorials for guided learning
- how-to guides for task completion
- reference docs for factual API/material
- explanation docs for design reasoning

Public-facing modules and types should usually have comments explaining:

- what they are for
- why they exist
- how they fit with the rest of the system when that is not obvious

API-level doc comments should be concise and useful:

- explain what a function/type does if it is not obvious from the signature
- explain why it exists when that context helps the reader
- include compact examples at module level where appropriate
- help readers skim the API surface without reading implementations

Do not add doc comments mechanically to every public function if they add no value.
Do not allow meaningful public surfaces to lag behind documentation.

Generating API docs is important and should be supported by the project structure.

## Plan And Task Tracking

Agents must keep planning and task tracking current.

Rules:

- `PLAN.md` is the current project plan and must stay up to date.
- Update `PLAN.md` when the direction changes, when the work order changes, or when a major planned step is completed.
- `TASKS.md` is the execution checklist.
- `TASKS.md` must use numbered tasks that are explicit, concrete, and easy to follow.
- Tasks in `TASKS.md` must be checkable so the current task can be referenced directly by number.
- Mark completed tasks with checkboxes.
- Compact or remove tasks when their detail is already carried clearly by the code, docs, or other stable project artifacts.
- When work materially advances, update `PLAN.md` and `TASKS.md` in the same change when relevant.

## Change Control

Agents must ask before:

- adding dependencies
- changing target frameworks
- introducing unsafe code
- adding benchmark projects or benchmark infrastructure
- changing established public API shape in a substantial way
- creating extra projects

When in doubt, ask before widening scope.

## Tone And Writing Style

Code, docs, and commit-ready prose should be:

- practical
- plain
- terse where possible
- technical when needed

They should not be:

- chatty
- congratulatory
- padded with generic framing
- written like a transcript of a conversation

## Decision Standard

When several approaches are possible, agents should prefer the one that best preserves:

1. consistency
2. developer experience
3. architectural clarity
4. performance in realistic binary workloads
5. minimal necessary abstraction

If an approach improves one small area while making the system less coherent, do not choose it without strong evidence.
