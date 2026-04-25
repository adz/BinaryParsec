namespace BinaryParsec.Protocols.Elf

open System.Runtime.CompilerServices

/// One ELF header together with the first program-header entry.
[<Struct; IsReadOnlyAttribute>]
type ElfFile =
    {
        Header: ElfFileHeader
        FirstProgramHeader: ElfProgramHeader
    }
