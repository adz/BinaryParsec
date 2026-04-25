namespace BinaryParsec.Protocols.Elf

/// Identifies the ELF file classes that the current package handles.
[<RequireQualifiedAccess>]
type ElfClass =
    | Elf32 = 1
    | Elf64 = 2
