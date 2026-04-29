namespace BinaryParsec.Protocols.Elf

open System.Runtime.CompilerServices

/// Structural ELF header fields needed to locate and read program-header entries.
///
/// The package intentionally keeps these container-level fields separate from
/// later segment interpretation so callers can decide how much of the program
/// header table they want to inspect.
[<Struct; IsReadOnlyAttribute>]
type ElfFileHeader =
    {
        Class: ElfClass
        DataEncoding: ElfDataEncoding
        EntryPoint: uint64
        ProgramHeaderOffset: uint64
        ProgramHeaderEntrySize: uint16
        ProgramHeaderEntryCount: uint16
    }
