namespace BinaryParsec.Protocols.Png

open System.Runtime.CompilerServices

/// The interpreted contents of the required PNG `IHDR` chunk.
[<Struct; IsReadOnlyAttribute>]
type PngImageHeader =
    {
        Width: uint32
        Height: uint32
        BitDepth: byte
        ColorType: PngColorType
        CompressionMethod: byte
        FilterMethod: byte
        InterlaceMethod: PngInterlaceMethod
    }
