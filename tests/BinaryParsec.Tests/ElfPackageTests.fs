namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Protocols.Elf
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module ElfPackageTests =
    let private littleEndianElf32WithTwoProgramHeaders =
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
            0x34uy; 0x00uy
            0x20uy; 0x00uy
            0x02uy; 0x00uy
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
            0x01uy; 0x00uy; 0x00uy; 0x00uy
            0x00uy; 0x00uy; 0x00uy; 0x00uy
            0x00uy; 0x20uy; 0x00uy; 0x00uy
            0x00uy; 0x20uy; 0x00uy; 0x00uy
            0x10uy; 0x00uy; 0x00uy; 0x00uy
            0x10uy; 0x00uy; 0x00uy; 0x00uy
            0x06uy; 0x00uy; 0x00uy; 0x00uy
            0x00uy; 0x20uy; 0x00uy; 0x00uy
        |]

    let private bigEndianElf64 =
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
            0x00uy; 0x40uy
            0x00uy; 0x38uy
            0x00uy; 0x01uy
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

    let private firstWritableLoadSegment =
        Contiguous.parse {
            let! header = Elf.header
            let! first = Elf.programHeaderAt header 0
            let! second = Elf.programHeaderAt header 1

            if first.Type = 1u && (first.Flags &&& 0x2u) <> 0u then
                return first
            elif second.Type = 1u && (second.Flags &&& 0x2u) <> 0u then
                return second
            else
                let! position = Contiguous.position
                return! ContiguousParser<_>(fun _ _ -> Contiguous.failAt position "Expected a writable PT_LOAD program header.")
        }

    [<Fact>]
    let ``header reads elf32 metadata needed for later table lookup`` () =
        match Elf.tryParseHeader (ReadOnlySpan<byte>(littleEndianElf32WithTwoProgramHeaders)) with
        | Ok header ->
            Assert.Equal(ElfClass.Elf32, header.Class)
            Assert.Equal(ElfDataEncoding.LittleEndian, header.DataEncoding)
            Assert.Equal(0x12345678UL, header.EntryPoint)
            Assert.Equal(0x34UL, header.ProgramHeaderOffset)
            Assert.Equal(0x20us, header.ProgramHeaderEntrySize)
            Assert.Equal(0x02us, header.ProgramHeaderEntryCount)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``file follows the elf64 program-header offset and reads flags with big-endian layout`` () =
        match Elf.tryParseFile (ReadOnlySpan<byte>(bigEndianElf64)) with
        | Ok file ->
            Assert.Equal(ElfClass.Elf64, file.Header.Class)
            Assert.Equal(ElfDataEncoding.BigEndian, file.Header.DataEncoding)
            Assert.Equal(0x0123456789ABCDEFUL, file.Header.EntryPoint)
            Assert.Equal(0x40UL, file.Header.ProgramHeaderOffset)
            Assert.Equal(1u, file.FirstProgramHeader.Type)
            Assert.Equal(0x5u, file.FirstProgramHeader.Flags)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``programHeaderAt lets later program-header interpretation stay outside the package tokenizer`` () =
        match Contiguous.run firstWritableLoadSegment (ReadOnlySpan<byte>(littleEndianElf32WithTwoProgramHeaders)) with
        | Ok programHeader ->
            Assert.Equal(1u, programHeader.Type)
            Assert.Equal(0x6u, programHeader.Flags)
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    [<Fact>]
    let ``header rejects unsupported data encodings at the EI_DATA byte`` () =
        let input =
            [|
                0x7Fuy; 0x45uy; 0x4Cuy; 0x46uy
                0x02uy; 0x03uy; 0x01uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
            |]

        match Elf.tryParseHeader (ReadOnlySpan<byte>(input)) with
        | Ok header ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{header}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 5 0 @>
            test <@ error.Message = "ELF data encoding 3 is not supported by this package." @>

    [<Fact>]
    let ``file rejects inputs without a program-header table`` () =
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
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x00uy; 0x00uy; 0x00uy; 0x00uy
                0x34uy; 0x00uy
                0x20uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
                0x00uy; 0x00uy
            |]

        match Elf.tryParseFile (ReadOnlySpan<byte>(input)) with
        | Ok file ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got %A{file}"))
        | Error error ->
            test <@ error.Position = ParsePosition.create 46 0 @>
            test <@ error.Message = "ELF file does not contain a program header table." @>
