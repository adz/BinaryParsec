# Plan

## Current Direction

The project is still in the foundation stage.

The immediate goal is to prove the contiguous-input core before adding broader parsing features or extra projects.

The repository baseline now assumes the .NET 10 SDK and the standard repo-root `artifacts/` output layout.

Documentation is being organized around the Divio split, with current design notes under `docs/explanation/`.

The current sequence is:

1. define the contiguous-input model in an explanation doc
2. replace the placeholder core with the minimum low-level model
3. add the first primitive read operations and tests
4. add the thinnest useful composition layer
5. pressure the design with PNG
6. pressure the design with Modbus RTU
7. use CAN as the next protocol pressure test once the first two slices prove the core

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

- `docs/explanation/contiguous-input-model.md`
- a minimal contiguous-input parser core
- primitive parsing tests
- a small PNG parser slice
- a small Modbus RTU parser slice

## Update Rule

This file must stay current.

When the direction changes, when a major task is completed, or when the order of work changes, update this file in the same piece of work.
