# PNG Package Reference

`BinaryParsec.Protocols.Png` exposes both zero-copy PNG chunk tokenizers and a validated file-level parser.

## Public entry points

- `Png.signature`
  Matches the fixed 8-byte PNG signature and returns its slice.
- `Png.chunkEnvelope`
  Reads one PNG chunk envelope as zero-copy slices.
- `Png.initialSlice`
  Parses the PNG signature and first chunk as a small fixed-shape slice.
- `Png.chunkStream`
  Walks chunk envelopes from the signature through `IEND`.
- `Png.file`
  Parses one whole PNG datastream into a validated `PngFile`.
- `Png.tryParseFile(ReadOnlySpan<byte>)`
  Returns `ParseResult<PngFile>`.
- `Png.parseFile(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid PNG input.

## Public models

### `PngChunkEnvelope`

- `Length`
  The declared PNG chunk data length.
- `ChunkType`
  The four-byte chunk type slice.
- `Payload`
  The zero-copy slice for the chunk data bytes.
- `Crc`
  The zero-copy slice for the stored chunk CRC field.

### `PngSlice`

- `Signature`
  The PNG signature slice.
- `FirstChunk`
  The first chunk envelope after the signature.

### `PngChunkStream`

- `Signature`
  The PNG signature slice.
- `Chunks`
  The zero-copy chunk envelopes in file order through `IEND`.

### `PngImageHeader`

- `Width`
  The `IHDR` image width in pixels.
- `Height`
  The `IHDR` image height in pixels.
- `BitDepth`
  The PNG bit depth from `IHDR`.
- `ColorType`
  The interpreted PNG color type.
- `CompressionMethod`
  The `IHDR` compression method byte.
- `FilterMethod`
  The `IHDR` filter method byte.
- `InterlaceMethod`
  The interpreted PNG interlace method.

### `PngChunk`

- `ChunkType`
  The ASCII chunk type code such as `IHDR`, `IDAT`, or `IEND`.
- `Data`
  The owned chunk data bytes.
- `Crc`
  The stored chunk CRC value.

### `PngFile`

- `Header`
  The interpreted `IHDR` data.
- `Chunks`
  The materialized chunk list in file order, including `IHDR` and `IEND`.
