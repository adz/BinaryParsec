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
2. [x] Make fixed-shape parser composition allocation-free in the core.
   The core now exposes allocation-safe fixed-shape combinators and `and!` CE lowering, with zero-allocation tests covering both paths.
3. [x] Move PNG and Modbus fixed-shape hot parsers onto the cleaner composition path.
   Update `Png.initialSlice` and the fixed-shape part of `ModbusRtuParser.frame` once the core composition path is allocation-safe.
4. [x] Cover repeated bounded reads with a tiny PNG chunk-iterator snippet.
   Drive reusable signature matching, chunk iteration, and length-bounded looping from PNG before returning to a fuller PNG package.
5. [x] Cover packed flags and multi-bit extraction with a tiny CAN classic frame snippet.
   Add only the bitfield helpers needed to parse identifier bits, control flags, and DLC correctly.
6. [x] Cover varints and length-delimited payloads with a tiny Protocol Buffers wire-format snippet.
   Add only the varint, tag, and bounded payload helpers needed for one realistic field reader and unknown-field skipping.
7. [x] Cover arbitrary-width bit extraction with a tiny DEFLATE block-prelude snippet.
   Use it to validate bit ordering, non-byte-aligned reads, and compact flag parsing.
8. [x] Cover width/endian completeness and offset-based reads with a tiny ELF snippet.
   Add the missing fixed-width integer readers and the minimum offset/jump helpers needed for header-plus-table parsing.
9. [x] Cover transport-plus-payload layering with a tiny Modbus TCP MBAP snippet.
   Use it to prove shared payload parsing over a distinct transport frame without widening the public API prematurely.
10. [x] Cover stateful byte-stream parsing with a tiny MIDI event snippet.
   Use it to pressure parser state and to inform the later incremental-input backend design.
11. [x] Flesh out docs in parallel with each snippet milestone.
   Add one explanation page, one how-to, and the relevant reference updates whenever a new capability family lands.
12. [x] Rework the existing Modbus package to follow the clarified package rules end to end.
   Keep the C#-friendly layer fully outside the core, pull in authoritative Modbus source material in a non-core location, use layout comments where they make RTU and TCP frame/token boundaries easier to follow, and keep transport tokenization separate from later PDU processing.
13. [x] Expand the existing PNG package into a fuller format package under the clarified rules.
   Keep spec and format references outside the core, preserve clear separation between chunk tokenization and later PNG processing, and use layout comments only where the byte structure benefits from them.
14. [x] Promote CAN from snippet coverage into a dedicated package.
   Build it from authoritative CAN framing references stored outside the core and keep the package as a thin consumer over the matured bit/byte primitives.
15. [ ] Promote the Protocol Buffers wire-format snippet into a dedicated package only if the package boundary stays clean.
   Pull in the relevant wire-format specification material outside the core, keep field tokenization separate from message-level processing, and avoid widening the core unless a real package scenario forces it.
16. [ ] Promote the DEFLATE prelude snippet into a dedicated package only if the package boundary stays clean.
   Use the relevant format specification material outside the core and keep bit-level tokenization clearly separated from later block semantics.
17. [ ] Promote the ELF snippet into a dedicated package only if the package boundary stays clean.
   Use the ABI/spec sources outside the core and keep structural token reads separate from later header/table interpretation.
18. [ ] Promote the MIDI event snippet into a dedicated package only if the package boundary stays clean.
   Pull in the relevant MIDI technical references outside the core and keep event tokenization and running-status state handling visually distinct from later event interpretation.
