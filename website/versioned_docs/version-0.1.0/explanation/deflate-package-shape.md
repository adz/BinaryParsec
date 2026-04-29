# DEFLATE Package Shape

`BinaryParsec.Protocols.Deflate` promotes the earlier DEFLATE prelude snippet into a package without turning the core or the package into a full inflater.

The package is intentionally split into two layers:

- packed DEFLATE block-header and dynamic-prelude tokenization over the contiguous core
- later Huffman-table and block-payload semantics outside that tokenizer

That split matters because the real BinaryParsec pressure from DEFLATE is still at the packed-bit boundary:

- least-significant-bit-first extraction
- non-byte-aligned field reads
- reserved block-type validation
- the dynamic-Huffman count fields that shape later decoding

Those reads are useful on their own for inspection, validation, and as the front edge of a fuller DEFLATE parser. They also keep the implementation visually close to RFC 1951 instead of mixing low-level bit packing with later Huffman semantics.

The tokenizer should only know how to read:

- the common `BFINAL` and `BTYPE` fields
- the reserved-versus-supported block-type distinction
- the `HLIT`, `HDIST`, and `HCLEN` counts for dynamic Huffman blocks

The later processing layer can then decide what to do with that metadata:

- read the code-length alphabet entries
- construct the literal/length and distance Huffman tables
- decode block payload symbols
- combine multiple blocks into a whole DEFLATE stream

Keeping those concerns separate preserves a clean package boundary. The core stays unchanged, the package remains a thin consumer over the existing bit primitives, and future DEFLATE work can grow above the prelude tokenizer only when the next real package task justifies it.

The current package deliberately stops short of:

- decoding stored-block padding and `LEN`/`NLEN`
- reading the full dynamic code-length alphabet
- building Huffman tables
- inflating literal/length and distance symbols across whole blocks or streams

That boundary is intentional. The package promotes the DEFLATE snippet into a real consumer while keeping BinaryParsec focused on binary tokenization and clean parser layering.
