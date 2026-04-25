namespace BinaryParsec.Protocols.Elf

/// Identifies the ELF byte-order encodings that the current package handles.
[<RequireQualifiedAccess>]
type ElfDataEncoding =
    | LittleEndian = 1
    | BigEndian = 2
