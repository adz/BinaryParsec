# Parse a PNG File

Use `BinaryParsec.Protocols.Png.Png` when application code needs a validated static PNG datastream parser over the core contiguous input model.

## Result-based parsing

```fsharp
open System
open BinaryParsec.Protocols.Png

match Png.tryParseFile (ReadOnlySpan pngBytes) with
| Ok png ->
    printfn "size=%dx%d chunks=%d" png.Header.Width png.Header.Height png.Chunks.Length
| Error error ->
    printfn "parse failed at byte %d: %s" error.Position.ByteOffset error.Message
```

## Throwing convenience parsing

```fsharp
open System
open BinaryParsec.Protocols.Png

let png = Png.parseFile (ReadOnlySpan pngBytes)
```

`parseFile` raises `InvalidDataException` when the input is not one valid PNG datastream.

## Notes

- `Png.file` validates CRCs and the core static-PNG chunk ordering rules before materializing the file model.
- `Png.chunkStream` is the lower-level zero-copy option when chunk boundaries matter more than a stable owned model.
- `PngFile.Header` is parsed from `IHDR`, while `PngFile.Chunks` preserves the full materialized chunk sequence.
- The current package targets static PNG framing rather than APNG playback semantics.
