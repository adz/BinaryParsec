namespace BinaryParsec.Protocols.Deflate

open BinaryParsec

[<RequireQualifiedAccess>]
module internal DeflateParser =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let blockHeader =
        Contiguous.parse {
            let! headerPosition = Contiguous.position
            let! isFinalBlock = Contiguous.bitsLsbFirst 1
            let! rawBlockType = Contiguous.bitsLsbFirst 2

            match rawBlockType with
            | 0u
            | 1u
            | 2u ->
                return
                    { IsFinalBlock = isFinalBlock = 1u
                      BlockType = enum<DeflateBlockType> (int rawBlockType) }
            | 3u ->
                return!
                    failAt
                        headerPosition
                        "DEFLATE block type 3 is reserved and cannot appear in a DEFLATE stream."
            | _ ->
                return! failAt headerPosition $"DEFLATE block type {rawBlockType} is out of range."
        }

    /// Reads the RFC 1951 section 3.2.7 count fields and leaves the later
    /// code-length alphabet and Huffman tables to higher-level parsing.
    let dynamicPrelude =
        Contiguous.parse {
            let! headerPosition = Contiguous.position
            let! header = blockHeader

            match header.BlockType with
            | DeflateBlockType.DynamicHuffman ->
                let! hlit = Contiguous.bitsLsbFirst 5
                let! hdist = Contiguous.bitsLsbFirst 5
                let! hclen = Contiguous.bitsLsbFirst 4

                return
                    { Header = header
                      LiteralLengthCodeCount = uint16 (hlit + 257u)
                      DistanceCodeCount = uint16 (hdist + 1u)
                      CodeLengthCodeCount = byte (hclen + 4u) }
            | _ ->
                let rawBlockType = int header.BlockType
                return! failAt headerPosition $"DEFLATE dynamic-block prelude requires BTYPE=2, got {rawBlockType}."
        }
