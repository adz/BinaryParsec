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
8. [x] Classify the parser surface into backend-neutral semantics versus contiguous-only conveniences.
   The goal is to prevent DX work from locking the library into the current contiguous runner while still preserving the readability gains.
9. [ ] Close the remaining hot-path DX regressions from the first additive pass.
   The build is clean and parser semantics are back in shape, but the computation-expression and a few package parsers still regress the zero-allocation tests and need a final allocation-focused pass.
10. [x] Stand up the Docusaurus docs site with bucketed navigation and versioned routing.
   The homepage, sidebar, and docs routes are arranged around Start, Integrate, Understand, Model, Examples, and Measure, and the site builds through standard Docusaurus.
11. [x] Add source-linked executable docs examples and the generation step that refreshes their captured output.
   The generation script runs the snippets, refreshes observed output, copies example/source files into the site, and syncs the `0.1.0` docs archive.
12. [ ] Rewrite the API reference pages as curated family hubs.
   Each major public family should explain what it is for, what can be done with it, which member groups matter, and what detail pages to read next.
13. [x] Add the integrations and measure sections as real navigation buckets.
   The integration pages focus on coexistence and migration stories, and the measure pages explain the zero-allocation checks and validation strategy.
14. [x] Sync the versioned docs archive and sidebar snapshot with the current docs tree.
   The generation script copies the current docs tree and sidebar snapshot into the `0.1.0` archive before Docusaurus builds.
15. [ ] Run the docs build, example generation, and repo tests as one consistency pass.
   Confirm the site renders, the examples produce the captured output, and the navigation no longer depends on namespace-first browsing as the main entry point.

## Modbus Interpreted Payloads

16. [ ] Define a comprehensive discriminated union for standard Modbus payloads.
   Include typed records for common public functions (0x01-0x06, 0x0F, 0x10) covering both requests and responses.
17. [ ] Implement sub-parsers for standard Modbus function payloads using the core syntax.
   Ensure strict validation of lengths, count-to-byte-count ratios, and address ranges according to the Modbus Application Protocol V1.1b3.
18. [ ] Add a protocol-level dispatcher that maps function codes to interpreted sub-parsers.
   Support a fallback to raw byte arrays for user-defined or unhandled function codes to preserve extensibility.
19. [ ] Introduce interpreted facades to `ModbusRtu` and `ModbusTcp`.
   Provide new `TryParse` and `Parse` entry points that return interpreted frame models while maintaining the existing raw-PDU API for low-level consumers.
20. [ ] Ensure interpreted payloads remain C#-friendly in the public surface.
   Use class-based hierarchies or tagged-union patterns that are idiomatic for C# consumption in the facade layer.
21. [ ] Add exhaustive test suites for interpreted Modbus payloads.
   Include valid frames, malformed payloads (e.g., mismatched byte counts), and exception responses for every supported function code.
22. [ ] Update Modbus package documentation and how-to guides.
   Showcase interpreted payload usage as the primary way to interact with registers, coils, and discrete inputs.
