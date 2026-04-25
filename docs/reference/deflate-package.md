# DEFLATE Package Reference

`BinaryParsec.Protocols.Deflate` currently exposes DEFLATE block-header tokenization plus the dynamic-Huffman count prelude over the shared contiguous parser core.

## Public entry points

- `Deflate.blockHeader`
  Parses one DEFLATE block header from the packed least-significant-bit-first bitstream.
- `Deflate.dynamicPrelude`
  Parses one DEFLATE dynamic-block prelude into the common block header plus the `HLIT`, `HDIST`, and `HCLEN` count metadata.

These entry points are exposed as parsers rather than `tryParse...` wrappers because DEFLATE block preludes end on bit boundaries, not necessarily byte boundaries.

## Public models

### `DeflateBlockType`

- `Uncompressed`
  Block type `0`.
- `FixedHuffman`
  Block type `1`.
- `DynamicHuffman`
  Block type `2`.

### `DeflateBlockHeader`

- `IsFinalBlock`
  Whether `BFINAL` is set for the current block.
- `BlockType`
  The decoded `BTYPE` value.

### `DeflateDynamicPrelude`

- `Header`
  The common DEFLATE block header.
- `LiteralLengthCodeCount`
  The decoded literal/length code count from `HLIT + 257`.
- `DistanceCodeCount`
  The decoded distance code count from `HDIST + 1`.
- `CodeLengthCodeCount`
  The decoded code-length alphabet count from `HCLEN + 4`.
