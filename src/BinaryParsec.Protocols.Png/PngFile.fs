namespace BinaryParsec.Protocols.Png

/// A stable owned PNG file model built from the PNG chunk stream.
type PngFile =
    {
        Header: PngImageHeader
        Chunks: PngChunk array
    }
