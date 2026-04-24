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

1. [x] Establish the baseline core, first consumers, and one production-ready protocol package.
   Core primitives, PNG slice, and Modbus RTU package-quality facade are in place.
2. [ ] Cover repeated bounded reads with a tiny PNG chunk-iterator snippet.
   Drive reusable signature matching, chunk iteration, and length-bounded looping from PNG before returning to a fuller PNG package.
3. [ ] Update the PNG parser implementation style.
   Replace waterfall-style chained parser code with clearer computation-expression composition where it does not violate hot-path constraints.
4. [ ] Cover packed flags and multi-bit extraction with a tiny CAN classic frame snippet.
   Add only the bitfield helpers needed to parse identifier bits, control flags, and DLC correctly.
5. [ ] Cover varints and length-delimited payloads with a tiny Protocol Buffers wire-format snippet.
   Add only the varint, tag, and bounded payload helpers needed for one realistic field reader and unknown-field skipping.
6. [ ] Cover arbitrary-width bit extraction with a tiny DEFLATE block-prelude snippet.
   Use it to validate bit ordering, non-byte-aligned reads, and compact flag parsing.
7. [ ] Cover width/endian completeness and offset-based reads with a tiny ELF snippet.
   Add the missing fixed-width integer readers and the minimum offset/jump helpers needed for header-plus-table parsing.
8. [ ] Cover transport-plus-payload layering with a tiny Modbus TCP MBAP snippet.
   Use it to prove shared payload parsing over a distinct transport frame without widening the public API prematurely.
9. [ ] Cover stateful byte-stream parsing with a tiny MIDI event snippet.
   Use it to pressure parser state and to inform the later incremental-input backend design.
10. [ ] Flesh out docs in parallel with each snippet milestone.
   Add one explanation page, one how-to, and the relevant reference updates whenever a new capability family lands.
11. [ ] Return to full protocol and format packages once the common reading paths are mostly covered.
   Resume CAN, expand PNG, and broaden Modbus/other packages using the matured core rather than growing them ad hoc.
