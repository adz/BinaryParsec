# Tasks

Use this file as the explicit execution checklist.

Rules:

- keep tasks numbered so they can be referenced directly
- keep tasks checkable
- keep tasks concrete enough to follow without reinterpretation
- mark completed tasks with `[x]`
- mark open tasks with `[ ]`
- compact or remove tasks when their detail is already carried clearly by the project
- update this file in the same change that materially advances the work

## Foundation

1. [x] Create `docs/explanation/contiguous-input-model.md`.
   Define the first intended shapes for input state, cursor movement, byte offset, bit offset, and failure position.
2. [x] Replace the placeholder core with the minimum low-level contiguous-input model.
   Keep only the essential types for position, parse error, and a contiguous-input runner.
3. [x] Implement `byte`, `peekByte`, `skip`, `take`, `u16be`, `u16le`, and one bit-reading primitive.
4. [x] Add tests for the first primitive operations.
   Cover successful reads, bounds failures, and byte/bit offset reporting.
5. [x] Add the thinnest useful composition layer.
   Introduce only enough parser composition to sequence primitives cleanly.

## First Real Consumers

6. [x] Create the initial PNG parser slice.
   Parse the file signature, chunk envelope, chunk length, and payload boundaries.
7. [x] Add validation coverage for the first PNG slice.
   Cover invalid signature, truncated chunk envelope, and invalid length handling.
8. [x] Create the initial Modbus RTU parser slice.
   Parse address, function code, payload bytes, and CRC result.
9. [x] Add validation coverage for the first Modbus RTU slice.
   Cover framing assumptions, CRC failures, and offset-aware diagnostics.

## Documentation

10. [x] Create the initial Divio folder structure under `docs/`.
11. [ ] Add module/type comments where they explain purpose and fit.
12. [ ] Add concise API comments where signatures alone are not enough.
13. [ ] Ensure generated API docs are supported by the project structure.

## Validation

14. [ ] Confirm the zero-allocation hot path over `ReadOnlySpan<byte>`.
15. [ ] Confirm the protocol-layer C# facade shape without distorting the core.
16. [ ] Review completed tasks and compact or remove obsolete detail that is already captured elsewhere in the repo.

## Repo Baseline

17. [x] Move SDK build output to the standard repo-root `artifacts/` layout.
18. [x] Upgrade the core project target framework to `net10.0`.
