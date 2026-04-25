namespace BinaryParsec.Protocols.Deflate

/// Identifies the three defined DEFLATE block kinds from RFC 1951 section 3.2.3.
[<RequireQualifiedAccess>]
type DeflateBlockType =
    | Uncompressed = 0
    | FixedHuffman = 1
    | DynamicHuffman = 2
