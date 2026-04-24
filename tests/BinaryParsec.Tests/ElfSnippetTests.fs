namespace BinaryParsec.Tests

open System
open BinaryParsec
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module ElfSnippetTests =
    [<Fact>]
    let ``elf32 little-endian snippet follows the program-header offset`` () =
        let input =
            [|
                0x7Fuy; 0x45uy; 0x4Cuy; 0x46uy
                0x01uy; 0x01uy; 0x01uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x02uy; 0x00uy
                0x03uy; 0x00uy
                0x01uy; 0x00uy; 0x00uy; 0x00uy
                0x78uy; 0x56uy; 0x34uy; 0x12uy
                0x34uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x01uy; 0x00uy; 0x00uy; 0x00uy
                0x78uy; 0x56uy; 0x34uy; 0x12uy
                0x00uy; 0x10uy; 0x00uy; 0x00uy
                0x00uy; 0x10uy; 0x00uy; 0x00uy
                0x20uy; 0x00uy; 0x00uy; 0x00uy
                0x20uy; 0x00uy; 0x00uy; 0x00uy
                0x05uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x10uy; 0x00uy; 0x00uy
            |]

        match Contiguous.run ElfSnippet.file (ReadOnlySpan<byte>(input)) with
        | Ok snippet ->
            test <@ snippet.Class = ElfClass.Elf32 @>
            test <@ snippet.DataEncoding = ElfDataEncoding.LittleEndian @>
            test <@ snippet.EntryPoint = 0x12345678UL @>
            test <@ snippet.ProgramHeaderOffset = 0x34UL @>
            test <@ snippet.FirstProgramHeaderType = 1u @>
            test <@ snippet.FirstProgramHeaderFlags = 0x5u @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``elf64 big-endian snippet covers 64-bit widths and big-endian layout`` () =
        let input =
            [|
                0x7Fuy; 0x45uy; 0x4Cuy; 0x46uy
                0x02uy; 0x02uy; 0x01uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x02uy
                0x00uy; 0x3Euy
                0x00uy; 0x00uy; 0x00uy; 0x01uy
                0x01uy; 0x23uy; 0x45uy; 0x67uy; 0x89uy; 0xABuy; 0xCDuy; 0xEFuy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x40uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x01uy
                0x00uy; 0x00uy; 0x00uy; 0x05uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x20uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x20uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy
            |]

        match Contiguous.run ElfSnippet.file (ReadOnlySpan<byte>(input)) with
        | Ok snippet ->
            test <@ snippet.Class = ElfClass.Elf64 @>
            test <@ snippet.DataEncoding = ElfDataEncoding.BigEndian @>
            test <@ snippet.EntryPoint = 0x0123456789ABCDEFUL @>
            test <@ snippet.ProgramHeaderOffset = 0x40UL @>
            test <@ snippet.FirstProgramHeaderType = 1u @>
            test <@ snippet.FirstProgramHeaderFlags = 0x5u @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``elf snippet rejects unsupported data encodings at the EI_DATA byte`` () =
        let input =
            [|
                0x7Fuy; 0x45uy; 0x4Cuy; 0x46uy
                0x02uy; 0x03uy; 0x01uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
            |]

        match Contiguous.run ElfSnippet.file (ReadOnlySpan<byte>(input)) with
        | Ok snippet ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{snippet}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 5 0 @>
            test <@ error.Message = "ELF data encoding 3 is not supported by this snippet." @>
