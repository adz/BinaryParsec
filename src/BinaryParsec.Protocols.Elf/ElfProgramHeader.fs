namespace BinaryParsec.Protocols.Elf

open System.Runtime.CompilerServices

/// Structural fields from one ELF program-header entry.
///
/// The current package reads only the entry type and flags because those are
/// enough to prove offset-based table lookup without taking on full segment
/// modeling.
[<Struct; IsReadOnlyAttribute>]
type ElfProgramHeader =
    {
        Type: uint32
        Flags: uint32
    }
