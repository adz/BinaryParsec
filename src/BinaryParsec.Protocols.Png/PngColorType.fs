namespace BinaryParsec.Protocols.Png

/// The PNG color type code from the `IHDR` chunk.
type PngColorType =
    | Greyscale = 0
    | Truecolor = 2
    | IndexedColor = 3
    | GreyscaleWithAlpha = 4
    | TruecolorWithAlpha = 6
