# ELF Package Reference

`BinaryParsec.Protocols.Elf` currently exposes ELF header tokenization plus indexed program-header lookup over the contiguous core.

## Public entry points

- `Elf.header`
  Parses one ELF header into `ElfFileHeader`.
- `Elf.programHeaderAt(header, index)`
  Parses one program-header entry at `index` using offsets from a previously parsed `ElfFileHeader`.
- `Elf.file`
  Parses one ELF header together with the first program-header entry.
- `Elf.tryParseHeader(ReadOnlySpan<byte>)`
  Returns `ParseResult<ElfFileHeader>`.
- `Elf.parseHeader(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid ELF header input.
- `Elf.tryParseFile(ReadOnlySpan<byte>)`
  Returns `ParseResult<ElfFile>`.
- `Elf.parseFile(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid ELF header or first-program-header input.

## Public models

### `ElfFileHeader`

- `Class`
  The parsed ELF class (`ELF32` or `ELF64`).
- `DataEncoding`
  The parsed ELF byte-order encoding.
- `EntryPoint`
  The `e_entry` value widened to `uint64`.
- `ProgramHeaderOffset`
  The `e_phoff` file offset.
- `ProgramHeaderEntrySize`
  The `e_phentsize` value.
- `ProgramHeaderEntryCount`
  The `e_phnum` value.

### `ElfProgramHeader`

- `Type`
  The program-header `p_type` value.
- `Flags`
  The program-header `p_flags` value.

### `ElfFile`

- `Header`
  The parsed `ElfFileHeader`.
- `FirstProgramHeader`
  The first program-header entry resolved through `Header.ProgramHeaderOffset`.

## Current scope

- The package handles `ELF32` and `ELF64` header layouts.
- The package handles little-endian and big-endian ELF container encodings.
- The package currently reads only the structural program-header fields needed for table lookup proof: `p_type` and `p_flags`.
- Machine-specific ABI details, section headers, relocation records, symbol tables, and loader policy remain outside the current package surface.
