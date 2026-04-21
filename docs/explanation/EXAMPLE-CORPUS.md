# Example Corpus

The example suite should force the API to cover both common and awkward binary parsing scenarios.

## Non-Protocol Formats

These are especially useful because they test the parsing model without dragging in transport/session concerns.

### PNG

What it drives:

- exact signatures
- chunked framing
- big-endian lengths
- CRC validation
- preservation of unknown chunk payloads

Why it matters:

It is a clean, realistic chunked binary format that stresses bounded subparsers and validation hooks.

### Protocol Buffers Wire Format

What it drives:

- varints
- field tags
- length-delimited submessages
- unknown field skipping
- repeated packed fields

Why it matters:

It stresses extensibility and efficient skipping without requiring schema-aware parsing in the core.

### CBOR

What it drives:

- major-type dispatch
- embedded length information
- tagged values
- indefinite-length forms
- recursive values

Why it matters:

It pushes compact dispatch and nested bounded parsing.

### ELF

What it drives:

- fixed headers
- endian switching
- offset-based table lookups
- 32-bit vs 64-bit layout differences

Why it matters:

It forces the design to accommodate offset-oriented parsing rather than only forward scanning.

### MIDI Event Stream

What it drives:

- byte-stream parsing
- running status
- variable-length quantities
- parser state between events

Why it matters:

It is a compact and realistic stress case for incremental parsing and stateful decoding.

### DEFLATE Block Prelude

What it drives:

- bit-order correctness
- arbitrary-width bit extraction
- compact flag handling

Why it matters:

If bit parsing works cleanly here, the bit-level API is probably on the right track.

## Industrial and Protocol-Oriented Formats

### Modbus RTU

What it drives:

- serial-style framing
- CRC validation
- function-code dispatch
- exception responses

### Modbus TCP

What it drives:

- transport framing distinct from the shared PDU
- validation of layering between transport and payload

### CAN

What it drives:

- compact frame metadata
- flag parsing
- payload-length rules

Why it matters:

It puts pressure on the bit-level and compact-frame parts of the design before more specialized variants such as CAN FD are added.

## Initial Ordered Implementation

1. PNG
2. Modbus RTU
3. CAN
4. Protocol Buffers wire parser
5. Modbus TCP

That set gives good coverage across chunked formats, checksums, compact binary frame parsing, varints, and transport/payload layering.
