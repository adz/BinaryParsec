namespace BinaryParsec.Protocols.Png

/// The PNG interlace method from the `IHDR` chunk.
type PngInterlaceMethod =
    | None = 0
    | Adam7 = 1
