# Parse an ELF Header and Program Header

Use `BinaryParsec.Protocols.Elf.Elf` when application code needs ELF container metadata and offset-based access to program-header entries.

## Result-based parsing

```fsharp
open System
open BinaryParsec.Protocols.Elf

let fileBytes = System.IO.File.ReadAllBytes("sample.elf")

match Elf.tryParseFile (ReadOnlySpan fileBytes) with
| Ok parsed ->
    printfn "class=%A endian=%A entry=0x%X first-phdr-type=%u"
        parsed.Header.Class
        parsed.Header.DataEncoding
        parsed.Header.EntryPoint
        parsed.FirstProgramHeader.Type
| Error error ->
    printfn "parse failed at byte %d bit %d: %s"
        error.Position.ByteOffset
        error.Position.BitOffset
        error.Message
```

## Indexed program-header lookup

```fsharp
open System
open BinaryParsec
open BinaryParsec.Protocols.Elf

let secondProgramHeader =
    Contiguous.parse {
        let! header = Elf.header
        return! Elf.programHeaderAt header 1
    }

match Contiguous.run secondProgramHeader (ReadOnlySpan fileBytes) with
| Ok programHeader ->
    printfn "type=%u flags=0x%X" programHeader.Type programHeader.Flags
| Error error ->
    printfn "lookup failed at byte %d bit %d: %s"
        error.Position.ByteOffset
        error.Position.BitOffset
        error.Message
```

## Notes

- `Elf.header` is the lower-level structural tokenizer when later table walking should stay in caller code.
- `Elf.programHeaderAt` uses `e_phoff` and `e_phentsize` from a previously parsed header instead of hard-coding one table layout.
- `Elf.file` is a convenience parser for the narrow current package scope: header plus first program-header entry.
- The current package does not yet model section headers, segment sizes, machine-specific ABI rules, or loader semantics.
