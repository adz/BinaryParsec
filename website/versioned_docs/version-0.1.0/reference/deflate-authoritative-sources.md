# DEFLATE Authoritative Sources

This page records the external DEFLATE sources that drive the current package implementation and tests.

The current `BinaryParsec.Protocols.Deflate` package intentionally targets only the packed block-header and dynamic-block prelude layer rather than full Huffman-table construction or payload inflation.

The primary source is:

- `RFC 1951: DEFLATE Compressed Data Format Specification version 1.3`
- Publisher: IETF
- Scope used here: least-significant-bit-first packing rules from section 3.1.1, block header meanings from section 3.2.3, and dynamic-Huffman prelude counts from section 3.2.7

Rules currently taken from this source:

- `BFINAL` is one low-order bit that marks whether the current block is the final block in the stream
- `BTYPE` is a two-bit packed field where `0`, `1`, and `2` are defined and `3` is reserved
- DEFLATE bit fields are read least-significant-bit first within the packed bitstream
- dynamic Huffman blocks carry `HLIT`, `HDIST`, and `HCLEN` count fields immediately after the common block header
- those count fields encode offsets from the real literal/length, distance, and code-length alphabet sizes

How this repository currently uses the source:

- `Deflate.blockHeader` tokenizes the common packed block header and rejects the reserved block type explicitly
- `Deflate.dynamicPrelude` reads the dynamic-Huffman count fields without pretending that Huffman-table decoding is the same concern
- later code-length and Huffman-table parsing stays outside the package tokenizer so package scope remains at the DEFLATE prelude boundary

Keep future DEFLATE work tied back to RFC 1951 rather than inferring bit ordering or count semantics casually.
