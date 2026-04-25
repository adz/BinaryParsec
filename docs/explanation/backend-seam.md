# Backend Seam

This note makes the next architecture constraint explicit:

do not treat the current contiguous runner as the final parser abstraction, and do not react by inventing a generalized streaming stack before one real consumer proves what must be shared.

The purpose of this seam is to preserve options.

## The Problem To Solve

The current core fuses two things too tightly:

- parser semantics
- contiguous execution mechanics

That fusion is cheap and clear for already-buffered inputs, but it creates the wrong pressure when a protocol also needs incremental or live transport parsing. Modbus RTU is the concrete example: packet captures fit the contiguous runner, while serial input eventually needs refill and partial-frame handling.

The next step is therefore not "replace the contiguous parser". The next step is "identify which parts of parsing should survive a backend change".

## What Must Stay Shared

The following concerns are parser semantics and should stay recognizable across backends:

- read a field and advance logical position
- branch on previously read values
- fail with an exact logical position
- keep tokenization separate from later validation or materialization
- preserve the package-level grammar shape for formats and protocols

For example, a Modbus RTU frame parser should still read as:

- address
- bounded PDU bytes
- CRC bytes
- CRC validation

Changing backend should not force package code to be rewritten into a different grammar vocabulary if the actual format logic is unchanged.

## What Must Stay Backend-Specific

The following concerns are execution mechanics and should not be prematurely flattened into fake symmetry:

- where bytes come from
- whether all requested bytes are already available
- whether EOF is final failure or incomplete input
- whether payload data can be borrowed or must be copied
- whether old bytes can be discarded while parsing continues
- what absolute offsets mean once a buffer window can slide

These differences are real. The architecture should expose a seam around them instead of pretending the same low-level payload and cursor model works equally well for contiguous buffers and live serial input.

## Surface Classification

To keep the architecture honest, we now classify the current parser surface into backend-neutral semantics and contiguous-only conveniences.

### Backend-Neutral Semantics

These concepts are expected to survive across execution backends. They define the "what" of parsing rather than the "how" of memory management.

- **Logical Flow**: `result`, `fail`, `failAt`, `map`, `map2`, `bind`, `zip`, `keepLeft`, `keepRight`, `parse` (CE).
- **Position Tracking**: `position` (abstract cursor), `remainingBytes` (where total size is known).
- **Primitive Value Reads**: `byte`, `u16be`, `u16le`, `u32be`, `u32le`, `u64be`, `u64le`, `varUInt64`.
- **Bit-Level Reads**: `bit`, `bits`, `bitsLsbFirst`.
- **Control**: `skip`, `expectBytes`.
- **Bounded Composition**: `parseExactly`, `parseRemaining`.

### Contiguous-Only Conveniences

These concepts rely on the presence of a single, stable, contiguous buffer in memory (`ReadOnlySpan<byte>`). They may not translate directly to streaming or non-contiguous backends without significant trade-offs (like copying or buffering).

- **Zero-Copy Slicing**: `ByteSlice` (the type itself), `takeSlice`, `takeRemaining`, `takeRemainingMinus`, `takeVarintSlice`.
- **Lookahead**: `peekByte` (requires at least one byte of reliable lookahead/buffering).
- **Absolute Offsets**: `readAt` (assumes random access across the entire input).
- **Runner API**: `ContiguousParser` (the delegate signature), `run`, `runExact` (the `ReadOnlySpan<byte>` entry points).

## Guardrails

Any backend work should satisfy all of these rules before new infrastructure lands:

1. Start from one concrete consumer.
   Right now that should be Modbus RTU over serial input.
2. Preserve the contiguous backend as the simple path.
   Do not regress file, capture, or already-buffered workloads just to prepare for streaming.
3. Share grammar-level flow before sharing low-level data shapes.
   Reuse parser meaning first. Reuse exact slice or cursor types only when the fit is real.
4. Do not standardize payload borrowing too early.
   `ByteSlice` is a good contiguous primitive. It is not automatically the right primitive for incremental input.
5. Add the minimum partial-input behavior needed by the concrete consumer.
   Do not jump to a broad async, pipeline, or refill framework before a narrow need proves it.

## Evaluation Questions

Before introducing a new backend abstraction, answer these questions in the relevant design change:

- Which package or scenario is blocked today?
- Which parser semantics are being preserved across backends?
- Which low-level mechanics are intentionally left backend-specific?
- Does the proposal let contiguous parsing stay the fast and obvious path for buffered input?
- Does the proposal reduce the need to rewrite package grammars, or only move complexity around?

If those answers are unclear, the abstraction is too early.

## Immediate Pressure Case

Use Modbus RTU over serial input as the first pressure case.

That case is useful because it is narrow and concrete:

- frame structure is already known
- CRC and PDU rules are already implemented
- the missing part is transport execution over partial input

That makes it a good test for separating grammar from backend without widening scope into a general streaming library.

## Non-Goals For The Next Step

The next step should not attempt to:

- design the final universal parser type
- add a second full public combinator surface
- unify contiguous and incremental payload exposure into one forced common type
- solve every future streaming scenario in one move

The goal is smaller: establish the seam clearly enough that later backend work can remain incremental, reviewable, and reversible.
