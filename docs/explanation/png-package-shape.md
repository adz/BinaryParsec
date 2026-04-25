# PNG Package Shape

`BinaryParsec.Protocols.Png` is the repository's non-protocol format package example.

The package is intentionally split into two layers:

- chunk tokenization over the contiguous core
- later PNG-specific validation and materialization

That split matters because PNG is naturally chunk-oriented. The tokenizer should only know how to read:

- the fixed PNG signature
- one chunk envelope
- repeated chunk envelopes until `IEND`

Those reads are useful on their own for zero-copy inspection and for continuing to pressure the core parser API with a real format.

The later processing layer then applies PNG rules that should not be mixed into the raw read path:

- `IHDR` interpretation
- CRC validation
- critical chunk ordering checks
- `PLTE` and `IDAT` relationship checks
- stable owned model construction

Keeping those concerns separate makes the package easier to review and extend. It also keeps the tokenization path visually close to the datastream layout while moving semantic checks into one place.

The current package deliberately stops short of becoming a full decoder. It validates static PNG framing and exposes the parsed header plus materialized chunks, but it does not yet:

- decompress `IDAT`
- reverse PNG scanline filters
- reconstruct pixels
- implement APNG animation semantics

That boundary keeps the package aligned with the current roadmap. It is already a fuller format package than the earlier slice-only pressure test, but it still grows from real parsing needs instead of speculative decoder infrastructure.
