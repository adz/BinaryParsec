namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Deflate
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module DeflatePackageTests =
    let private dynamicCodeLengthOrder =
        [|
            16; 17; 18; 0; 8; 7; 9; 6; 10; 5; 11; 4; 12; 3; 13; 2; 14; 1; 15
        |]

    let rec private codeLengthEntries
        (index: int)
        (count: int)
        (codeLengths: byte array)
        : ContiguousParser<byte array> =
        Contiguous.parse {
            if index >= count then
                return codeLengths
            else
                let! codeLength = Contiguous.bitsLsbFirst 3
                codeLengths[dynamicCodeLengthOrder[index]] <- byte codeLength
                return! codeLengthEntries (index + 1) count codeLengths
        }

    let private dynamicCodeLengthCodeLengths =
        Contiguous.parse {
            let! prelude = Deflate.dynamicPrelude
            let codeLengths: byte array = Array.zeroCreate 19
            let! codeLengths = codeLengthEntries 0 (prelude.CodeLengthCodeCount |> int) codeLengths
            return prelude, codeLengths
        }

    [<Fact>]
    let ``blockHeader reads a fixed Huffman header from lsb-first packed bits`` () =
        let input = [| 0x03uy |]

        match Contiguous.run Deflate.blockHeader (ReadOnlySpan<byte>(input)) with
        | Ok header ->
            test <@ header.IsFinalBlock @>
            test <@ header.BlockType = DeflateBlockType.FixedHuffman @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``dynamicPrelude reads lsb-first packed fields across byte boundaries`` () =
        let input = [| 0xEDuy; 0xCDuy; 0x01uy |]

        match Contiguous.run Deflate.dynamicPrelude (ReadOnlySpan<byte>(input)) with
        | Ok prelude ->
            test <@ prelude.Header.IsFinalBlock @>
            test <@ prelude.Header.BlockType = DeflateBlockType.DynamicHuffman @>
            test <@ prelude.LiteralLengthCodeCount = 286us @>
            test <@ prelude.DistanceCodeCount = 14us @>
            test <@ prelude.CodeLengthCodeCount = 18uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``dynamicPrelude rejects non-dynamic block types at the block header`` () =
        let input = [| 0x03uy |]

        match Contiguous.run Deflate.dynamicPrelude (ReadOnlySpan<byte>(input)) with
        | Ok prelude ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{prelude}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "DEFLATE dynamic-block prelude requires BTYPE=2, got 1." @>

    [<Fact>]
    let ``blockHeader rejects reserved block type 3`` () =
        let input = [| 0x06uy |]

        match Contiguous.run Deflate.blockHeader (ReadOnlySpan<byte>(input)) with
        | Ok header ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{header}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "DEFLATE block type 3 is reserved and cannot appear in a DEFLATE stream." @>

    [<Fact>]
    let ``later dynamic code-length parsing can stay separate from the package tokenizer`` () =
        let input =
            [|
                0xEDuy; 0xCDuy; 0x01uy
                0x4Duy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
            |]

        match Contiguous.run dynamicCodeLengthCodeLengths (ReadOnlySpan<byte>(input)) with
        | Ok(prelude, codeLengths) ->
            test <@ prelude.CodeLengthCodeCount = 18uy @>
            test <@ codeLengths[16] = 0uy @>
            test <@ codeLengths[17] = 0uy @>
            test <@ codeLengths[18] = 2uy @>
            test <@ codeLengths[0] = 3uy @>
            test <@ codeLengths[8] = 2uy @>
            test <@ codeLengths[7] = 0uy @>
            test <@ codeLengths[1] = 0uy @>
            test <@ codeLengths[15] = 0uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))
