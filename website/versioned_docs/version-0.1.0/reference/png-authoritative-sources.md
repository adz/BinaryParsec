# PNG Authoritative Sources

This page records the external PNG sources that drive the current package implementation and tests.

The primary source is:

- `Portable Network Graphics (PNG) Specification (Third Edition)`
- Publisher: W3C
- Status: Recommendation
- Date: June 24, 2025
- URL: `https://www.w3.org/TR/png-3/`

The current `BinaryParsec.Protocols.Png` package intentionally implements the shared static-PNG framing rules that remain central across editions, even though the 2025 W3C Recommendation also covers APNG and newer ancillary chunks.

Rules currently taken from this source:

- the PNG datastream begins with the fixed 8-byte PNG signature
- the signature is immediately followed by an `IHDR` chunk and the datastream ends with an `IEND` chunk
- no bytes follow the `IEND` chunk in a conforming PNG datastream
- every chunk carries a CRC over the chunk type bytes plus the chunk data bytes
- the `IHDR` chunk payload is exactly 13 bytes
- `IHDR` width and height are non-zero and limited to `2^31 - 1`
- valid `IHDR` color types are `0`, `2`, `3`, `4`, and `6`
- valid bit-depth combinations depend on the color type
- compression method `0` and filter method `0` are the only currently defined values
- interlace methods `0` and `1` are the only currently defined values
- a valid PNG datastream contains at least one `IDAT` chunk
- multiple `IDAT` chunks must be consecutive
- indexed-color images require `PLTE`
- greyscale images must not contain `PLTE`
- `IEND` has an empty payload

How this repository currently uses the source:

- `Png.chunkEnvelope` and `Png.chunkStream` implement chunk tokenization directly from the datastream layout
- `Png.file` keeps later validation separate from tokenization and materializes a stable owned PNG model
- PNG tests use small static datastreams that exercise signature handling, CRC validation, `IHDR` decoding, chunk ordering, and required critical chunks

Keep new PNG work tied back to this source rather than inferring chunk rules casually.
