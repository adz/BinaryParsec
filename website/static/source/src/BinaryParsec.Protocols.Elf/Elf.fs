namespace BinaryParsec.Protocols.Elf

open System
open System.IO
open BinaryParsec

/// ELF header and program-header tokenizers over the contiguous core.
///
/// The current package intentionally stays at the structural container layer.
/// It reads the ELF header plus indexed program-header entries while leaving
/// machine-specific ABI semantics, section parsing, and loader policy to
/// higher-level consumers.
[<RequireQualifiedAccess>]
module Elf =
    /// Parses one ELF header from the start of contiguous input.
    let header = ElfParser.header

    /// Parses one program-header entry by index using offsets from a previously parsed ELF header.
    let programHeaderAt (header: ElfFileHeader) (index: int) =
        ElfParser.programHeaderAt header index

    /// Parses one ELF header together with the first program-header entry.
    let file = ElfParser.file

    /// Parses one ELF header from the start of the input.
    let tryParseHeader (input: ReadOnlySpan<byte>) : ParseResult<ElfFileHeader> =
        Contiguous.run header input

    /// Parses one ELF header or raises `InvalidDataException` when the input is invalid.
    let parseHeader (input: ReadOnlySpan<byte>) : ElfFileHeader =
        match tryParseHeader input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn

    /// Parses one ELF header and its first program-header entry from the start of the input.
    let tryParseFile (input: ReadOnlySpan<byte>) : ParseResult<ElfFile> =
        Contiguous.run file input

    /// Parses one ELF header and its first program-header entry or raises `InvalidDataException` when the input is invalid.
    let parseFile (input: ReadOnlySpan<byte>) : ElfFile =
        match tryParseFile input with
        | Ok parsed -> parsed
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn
