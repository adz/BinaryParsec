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

1. [x] Audit the current core surface, introductory docs, and package parsers for DX friction.
   The main issues are repetitive `Contiguous.` qualification, unclear distinction between decoded values and slices, parser names that only make sense after explanation, and examples that foreground machinery instead of binary structure.
2. [x] Switch the active plan from the deferred streaming seam to a DX-first improvement pass.
   The repo direction should now explicitly prioritize readability, parser ergonomics, and spec-shaped parser code before more backend expansion.
3. [x] Add an additive low-ceremony parser-writing surface over the existing contiguous core.
   Provide a recommended style that removes most `Contiguous.` noise while keeping byte consumption, offsets, and failure positions explicit.
4. [x] Add clearer names for slice-taking and bounded nested parsing operations.
   The API should make it obvious when code is decoding a value, carving out raw bytes, or parsing a bounded sub-message.
5. [x] Refactor representative protocol parsers to the clearer style and keep the improved code aligned with their binary layouts.
   Start with length-prefixed or framed examples where the current surface hides intent, then extend to at least one bit-oriented and one offset-oriented parser.
6. [x] Update the docs so the recommended parser style is the first thing new users see.
   The tutorial, how-to, and reference paths should explain the mental model, show the preferred surface, and explain when to use slices versus nested parsing.
7. [x] Add tests for the new DX surface and keep the zero-allocation guarantees for the underlying hot-path primitives.
   Cover aliases or wrappers, bounded nested parsing, clearer error mapping, and at least one end-to-end parser example that reads close to its source format.
8. [ ] Classify the parser surface into backend-neutral semantics versus contiguous-only conveniences.
   The goal is to prevent DX work from locking the library into the current contiguous runner while still preserving the readability gains.
9. [ ] Reassess the remaining DX gaps after the first additive pass.
   Decide which issues are solved by naming and examples, which need more combinators, and whether any larger surface redesign is justified without compromising future streaming or non-contiguous backends.
