namespace BinaryParsec.Tests

open System
open BinaryParsec
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module DeflateBlockPreludeSnippetTests =
    [<Fact>]
    let ``dynamic prelude reads lsb-first packed fields across byte boundaries`` () =
        let input = [| 0xEDuy; 0xCDuy; 0x01uy |]

        match Contiguous.run DeflateBlockPreludeSnippet.dynamicPrelude (ReadOnlySpan<byte>(input)) with
        | Ok prelude ->
            test <@ prelude.IsFinalBlock @>
            test <@ prelude.BlockType = DeflateBlockType.DynamicHuffman @>
            test <@ prelude.LiteralLengthCodeCount = 286us @>
            test <@ prelude.DistanceCodeCount = 14us @>
            test <@ prelude.CodeLengthCodeCount = 18uy @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``dynamic prelude rejects non-dynamic block types at the block header`` () =
        let input = [| 0x06uy |]

        match Contiguous.run DeflateBlockPreludeSnippet.dynamicPrelude (ReadOnlySpan<byte>(input)) with
        | Ok prelude ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{prelude}"))
        | Error error ->
            test <@ error.Position = ParsePosition.origin @>
            test <@ error.Message = "DEFLATE dynamic-block prelude requires BTYPE=2, got 3." @>
