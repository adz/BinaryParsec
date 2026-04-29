# Parse a DEFLATE Dynamic Block Prelude

Use `BinaryParsec.Protocols.Deflate` when you want the packed DEFLATE block header and dynamic-Huffman count fields without pulling Huffman-table decoding into the package boundary.

## Parse one dynamic-block prelude

```fsharp
open System
open BinaryParsec
open BinaryParsec.Protocols.Deflate

let input = [| 0xEDuy; 0xCDuy; 0x01uy |]

match Contiguous.run Deflate.dynamicPrelude (ReadOnlySpan<byte>(input)) with
| Ok prelude ->
    printfn "final: %b" prelude.Header.IsFinalBlock
    printfn "block type: %A" prelude.Header.BlockType
    printfn "literal/length codes: %d" prelude.LiteralLengthCodeCount
    printfn "distance codes: %d" prelude.DistanceCodeCount
    printfn "code-length codes: %d" prelude.CodeLengthCodeCount
| Error error ->
    printfn "parse failed at %A: %s" error.Position error.Message
```

## Keep later block semantics above `Deflate.dynamicPrelude`

The package intentionally stops at the prelude boundary. If you need to read the dynamic code-length alphabet, build that on top of the package tokenizer:

```fsharp
open System
open BinaryParsec
open BinaryParsec.Protocols.Deflate

let dynamicCodeLengthOrder =
    [| 16; 17; 18; 0; 8; 7; 9; 6; 10; 5; 11; 4; 12; 3; 13; 2; 14; 1; 15 |]

let rec codeLengthEntries index count codeLengths =
    Contiguous.parse {
        if index >= count then
            return codeLengths
        else
            let! codeLength = Contiguous.bitsLsbFirst 3
            codeLengths[dynamicCodeLengthOrder[index]] <- byte codeLength
            return! codeLengthEntries (index + 1) count codeLengths
    }

let dynamicCodeLengthCodeLengths =
    Contiguous.parse {
        let! prelude = Deflate.dynamicPrelude
        let codeLengths = Array.zeroCreate 19

        let! codeLengths = codeLengthEntries 0 (prelude.CodeLengthCodeCount |> int) codeLengths
        return prelude, codeLengths
    }
```

That split is the intended package shape: `Deflate.dynamicPrelude` handles the packed DEFLATE prelude, and later Huffman-table logic stays above it.
