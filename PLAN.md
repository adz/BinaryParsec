# Plan

## Current Direction

The project is still in the foundation stage.

The immediate goal is to prove the contiguous-input core before adding broader parsing features or extra projects.

The repository baseline now assumes the .NET 10 SDK and the standard repo-root `artifacts/` output layout.

Documentation is being organized around the Divio split, with current design notes under `docs/explanation/`.

The current sequence is:

1. add the first primitive read operations and tests
2. add the thinnest useful composition layer
3. pressure the design with PNG
4. pressure the design with Modbus RTU
5. use CAN as the next protocol pressure test once the first two slices prove the core

## Why This Order

- It keeps the core grounded in real binary parsing needs.
- It proves the low-level model before stream support or broader abstractions appear.
- It uses one non-protocol format and one industrial protocol to shape the next design decisions.
- It keeps C# concerns at the protocol layer rather than distorting the core too early.

## Constraints

- Do not add stream support before the contiguous-input model is proven.
- Do not add advanced combinators before real parsers need them.
- Do not add extra projects before PNG and Modbus RTU show where the actual pressure is.
- Do not add C#-specific compromises to the core before a protocol package needs them.
- Keep build output in the repo-root `artifacts/` folder rather than project-local `bin/` and `obj/` paths.

## Planned First Deliverables

- a minimal contiguous-input parser core
- primitive parsing tests
- a small PNG parser slice
- a small Modbus RTU parser slice

## Current Status

- the placeholder `SpanParser` shell has been replaced with the minimum contiguous-input runner
- the core now models parser state explicitly as `ReadOnlySpan<byte>` plus `ParsePosition`
- the first byte, endian, slice, and bit primitives now sit on top of that runner
- the next task is to add validation coverage for the first primitive operations

## Update Rule

This file must stay current.

When the direction changes, when a major task is completed, or when the order of work changes, update this file in the same piece of work.
