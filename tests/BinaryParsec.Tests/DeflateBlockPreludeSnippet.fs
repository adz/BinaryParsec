namespace BinaryParsec.Tests

open BinaryParsec

/// Identifies the three defined DEFLATE block kinds from RFC 1951 section 3.2.3.
[<RequireQualifiedAccess>]
type DeflateBlockType =
    | Uncompressed = 0
    | FixedHuffman = 1
    | DynamicHuffman = 2

/// Captures the packed metadata from a tiny DEFLATE block prelude.
type DeflateBlockPreludeSnippet =
    {
        IsFinalBlock: bool
        BlockType: DeflateBlockType
        LiteralLengthCodeCount: uint16
        DistanceCodeCount: uint16
        CodeLengthCodeCount: byte
    }

[<RequireQualifiedAccess>]
module internal DeflateBlockPreludeSnippet =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    /// Parses the common dynamic-block prelude fields from RFC 1951 section 3.2.7.
    ///
    /// DEFLATE packs these fields least-significant-bit first, so the snippet
    /// exercises non-byte-aligned reads and bit-order correctness directly.
    let dynamicPrelude =
        Contiguous.parse {
            let! headerPosition = Contiguous.position
            let! isFinalBlock = Contiguous.bitsLsbFirst 1
            let! rawBlockType = Contiguous.bitsLsbFirst 2

            match rawBlockType with
            | 2u ->
                let! hlit = Contiguous.bitsLsbFirst 5
                let! hdist = Contiguous.bitsLsbFirst 5
                let! hclen = Contiguous.bitsLsbFirst 4

                return
                    {
                        IsFinalBlock = isFinalBlock = 1u
                        BlockType = DeflateBlockType.DynamicHuffman
                        LiteralLengthCodeCount = uint16 (hlit + 257u)
                        DistanceCodeCount = uint16 (hdist + 1u)
                        CodeLengthCodeCount = byte (hclen + 4u)
                    }
            | 0u
            | 1u
            | 3u ->
                return! failAt headerPosition $"DEFLATE dynamic-block prelude requires BTYPE=2, got {rawBlockType}."
            | _ ->
                return! failAt headerPosition $"DEFLATE block type {rawBlockType} is out of range."
        }
