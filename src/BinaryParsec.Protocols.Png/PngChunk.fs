namespace BinaryParsec.Protocols.Png

/// One materialized PNG chunk in file order.
type PngChunk =
    {
        ChunkType: string
        Data: byte array
        Crc: uint32
    }
