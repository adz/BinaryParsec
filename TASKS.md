# Tasks

Use this file as the active execution checklist.

Rules:

- keep tasks numbered so they can be referenced directly
- keep tasks checkable
- keep tasks concrete enough to follow without reinterpretation
- mark completed tasks with `[x]`
- mark open tasks with `[ ]`
- keep the list compact and focused on the current roadmap rather than preserving all project history
- update this file in the same change that materially advances the work

1. [x] Complete the snippet-to-package promotion pass without widening the core prematurely.
   PNG, Modbus, CAN, Protocol Buffers, DEFLATE, ELF, and MIDI now all live as dedicated package work where justified.
2. [ ] Add an explicit architecture guardrail against premature backend optimization and premature abstraction.
   Document and enforce that new execution-model work must preserve options, keep the contiguous backend simple, and avoid committing to a generalized streaming stack before one concrete consumer proves the seam.
3. [ ] Design the minimum parser/backend seam needed for real streaming consumers.
   Start from one concrete case, most likely Modbus RTU over serial input, and decide what must be shared between contiguous and incremental execution without forcing package authors to switch grammar-level parsers.
