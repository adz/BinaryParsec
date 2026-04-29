namespace BinaryParsec.Protocols.Deflate

open BinaryParsec
open BinaryParsec.Syntax

[<RequireQualifiedAccess>]
module internal DeflateParser =
    let blockHeader =
        parse {
            let! headerPosition = position
            let! isFinalBlock = bitsLsbFirst 1
            let! rawBlockType = bitsLsbFirst 2

            match rawBlockType with
            | 0u
            | 1u
            | 2u ->
                return
                    { IsFinalBlock = isFinalBlock = 1u
                      BlockType = enum<DeflateBlockType> (int rawBlockType) }
            | 3u ->
                return!
                    fail
                        headerPosition
                        "DEFLATE block type 3 is reserved and cannot appear in a DEFLATE stream."
            | _ ->
                return! fail headerPosition $"DEFLATE block type {rawBlockType} is out of range."
        }

    /// Reads the RFC 1951 section 3.2.7 count fields and leaves the later
    /// code-length alphabet and Huffman tables to higher-level parsing.
    let dynamicPrelude =
        parse {
            let! headerPosition = position
            let! header = blockHeader

            match header.BlockType with
            | DeflateBlockType.DynamicHuffman ->
                let! hlit = bitsLsbFirst 5
                let! hdist = bitsLsbFirst 5
                let! hclen = bitsLsbFirst 4

                return
                    { Header = header
                      LiteralLengthCodeCount = uint16 (hlit + 257u)
                      DistanceCodeCount = uint16 (hdist + 1u)
                      CodeLengthCodeCount = uint8 (hclen + 4u) }
            | _ ->
                let rawBlockType = int header.BlockType
                return! fail headerPosition $"DEFLATE dynamic-block prelude requires BTYPE=2, got {rawBlockType}."
        }
