# Modbus RTU Serial Seam

This note defines the minimum seam that should be tested before adding any generalized streaming backend to `BinaryParsec`.

The pressure case is Modbus RTU over serial input.

## The Important Observation

The current `ModbusRtuParser.frame` does not discover frame boundaries.

It assumes a complete RTU frame is already available as one contiguous input:

- address byte
- PDU bytes
- trailing CRC bytes

That means the parser is already downstream of transport framing.

For packet captures, tests, and already-buffered messages, that is fine. For live serial input, the missing piece is not immediately "a whole new parser backend". The missing piece is "how do we accumulate bytes until we have one candidate RTU frame to hand to the existing parser".

## What This Means Architecturally

The first seam to test is narrower than a general incremental parser core.

For Modbus RTU over serial input, the minimal architecture is:

1. serial transport buffering and frame-boundary detection
2. contiguous RTU frame parsing over the resulting candidate bytes
3. existing PDU parsing and materialization

In other words:

- live transport concerns stay at the package edge first
- the proven contiguous parser remains the consumer of one candidate frame
- the shared Modbus grammar stays in the existing RTU and PDU parsing code

This is a better first move than forcing the core into a universal incremental parser design before we know whether Modbus RTU actually needs one.

## Shared Semantics

The following logic should remain shared between buffered and serial RTU usage:

- CRC computation and mismatch reporting
- Modbus function-code parsing
- exception-response handling
- payload materialization
- offset-aware parse failures within a candidate frame

Those semantics already exist and should not be rewritten just because the source of bytes changes.

## Backend-Specific Mechanics

The following logic should remain specific to the serial transport edge until a stronger common pattern appears:

- how bytes are read from the serial source
- how candidate RTU frame boundaries are detected
- how partial frames are buffered between reads
- when buffered bytes are discarded
- how transport timing or delimiter policy is represented

Those concerns do not automatically belong in the core parser engine.

## Minimum Design Decision

The minimum design decision for the first streaming pressure case is:

- do not start by generalizing `ContiguousParser<'T>`
- add a package-level serial framing component that produces contiguous candidate RTU frames
- feed those candidate frames into the existing `ModbusRtu` parse path

Only if that package-level seam proves insufficient should the project widen into a reusable incremental core abstraction.

## Why This Preserves Options

This approach keeps three paths open:

- contiguous parsing remains the simple and fast path for buffered inputs
- Modbus RTU can support live serial input without waiting for a universal backend rewrite
- later incremental work can still move into the core if multiple packages end up needing the same execution mechanics

That is the right bias for the current project state. It solves the concrete pressure first and delays irreversible abstraction choices until real reuse appears.
