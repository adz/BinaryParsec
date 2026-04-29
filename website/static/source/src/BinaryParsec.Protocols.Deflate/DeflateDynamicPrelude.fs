namespace BinaryParsec.Protocols.Deflate

/// Captures the packed metadata that starts one DEFLATE dynamic Huffman block.
///
/// These counts describe the shape of the later Huffman-code description but
/// do not decode those later code-length entries themselves.
type DeflateDynamicPrelude =
    {
        Header: DeflateBlockHeader
        LiteralLengthCodeCount: uint16
        DistanceCodeCount: uint16
        CodeLengthCodeCount: byte
    }
