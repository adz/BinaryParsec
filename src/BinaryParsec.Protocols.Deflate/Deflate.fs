namespace BinaryParsec.Protocols.Deflate

/// DEFLATE block-header and dynamic-prelude tokenizers over the contiguous core.
///
/// The current package intentionally stops at the packed block-prelude layer.
/// It reads BFINAL, BTYPE, and the dynamic-Huffman count fields while leaving
/// Huffman-table construction and block payload decoding to later parsers.
[<RequireQualifiedAccess>]
module Deflate =
    /// Parses one DEFLATE block header from the packed least-significant-bit-first bitstream.
    let blockHeader = DeflateParser.blockHeader

    /// Parses one DEFLATE dynamic-block prelude and returns the packed count metadata.
    let dynamicPrelude = DeflateParser.dynamicPrelude
