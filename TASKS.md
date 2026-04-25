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
2. [x] Add an explicit architecture guardrail against premature backend optimization and premature abstraction.
   The architecture docs now state the backend seam directly, preserve the contiguous backend as the simple path, and require future execution-model work to name what is shared versus backend-specific before new infrastructure lands.
3. [x] Design the minimum parser/backend seam needed for the first real streaming consumer.
   The first seam is now defined around Modbus RTU over serial input: keep serial buffering and frame-boundary detection at the package edge, then feed candidate contiguous frames into the existing RTU and shared PDU parsers instead of widening the core first.
