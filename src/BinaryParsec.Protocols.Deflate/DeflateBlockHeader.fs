namespace BinaryParsec.Protocols.Deflate

/// Captures the common packed header shared by all DEFLATE blocks.
///
/// The package stops at the bit-level header boundary so later block decoding
/// can stay separate from the packed tokenization step.
type DeflateBlockHeader =
    {
        IsFinalBlock: bool
        BlockType: DeflateBlockType
    }
