# Plan

## Current Direction

The project is still in the foundation stage.

The immediate goal is to keep proving the contiguous-input core while using separate protocol projects to validate the package boundary.

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
- Keep protocol and format consumers outside the core project once they prove the boundary is warranted.
- Do not add C#-specific compromises to the core before a protocol package needs them.
- Keep build output in the repo-root `artifacts/` folder rather than project-local `bin/` and `obj/` paths.

## Planned First Deliverables

- a minimal contiguous-input parser core
- primitive parsing tests
- a small PNG parser slice
- a small Modbus RTU parser slice

## Current Status

- the core is now a contiguous `ReadOnlySpan<byte>` runner over explicit `ParsePosition`, with byte, endian, slice, and bit primitives
- the composition layer is in place with `map`, `bind`, sequencing helpers, and a computation-expression entry point
- the PNG and Modbus RTU slices now live in separate `BinaryParsec.Protocols.*` projects and are validated with offset-aware diagnostics
- the public surface now carries the needed purpose-and-fit comments and concise API docs
- builds stage assemblies and XML docs under `artifacts/api-docs/` for generated reference consumption
- successful hot paths for the primitive, PNG, and Modbus RTU slices stay allocation-free
- the protocol-layer C# direction is confirmed as thin `BinaryParsec.Protocols.*` facades over the F#-first core
- test coverage now runs through `dotnet test` in `BinaryParsec.Tests` with Unquote-backed assertions
- the next task is to pressure the core with CAN as the next protocol consumer

## Update Rule

This file must stay current.

When the direction changes, when a major task is completed, or when the order of work changes, update this file in the same piece of work.
