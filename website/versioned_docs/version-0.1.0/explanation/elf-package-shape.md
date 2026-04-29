# ELF Package Shape

`BinaryParsec.Protocols.Elf` promotes the earlier ELF snippet into a package without pretending to be a full executable loader.

The package is intentionally split into two layers:

- structural ELF header tokenization
- later caller-owned interpretation of indexed program-header entries

That split matters because the pressure from ELF is in the container layout itself:

- `EI_CLASS` selects `ELF32` versus `ELF64` field widths
- `EI_DATA` selects little-endian versus big-endian reads
- `e_phoff` and `e_phentsize` drive dependent table lookup
- the `ELF32` and `ELF64` program-header layouts place `p_flags` at different offsets

Those are the exact format pressures that originally justified the core width readers, endian-aware primitives, and absolute offset reads.

The package therefore stops at the structural layer that is broadly reusable:

- read the ELF header fields that identify width, endianness, entry point, and program-header table location
- expose indexed program-header lookup driven by the parsed header
- provide a thin convenience parser for the common "header plus first program header" case

The package deliberately leaves later interpretation outside the tokenizer:

- machine-specific ABI rules
- section-header parsing
- relocation and symbol-table handling
- full segment models and loader decisions

That boundary keeps the package honest. It promotes the ELF snippet into a dedicated consumer over the core without widening the core or smuggling executable-semantics policy into what should stay a structural container package.
