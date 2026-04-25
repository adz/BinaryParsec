# ELF Authoritative Sources

This page records the external ELF ABI sources that drive the current package implementation and tests.

The primary source is:

- `System V Application Binary Interface`
- Publisher: The Linux Foundation / generic ELF ABI reference
- Sections used here:
  `Chapter 4: ELF Header`
  `Chapter 5: Program Header`
- Reference pages:
  `https://refspecs.linuxfoundation.org/elf/gabi4+/ch4.eheader.html`
  `https://refspecs.linuxfoundation.org/elf/gabi4+/ch5.pheader.html`

Rules currently taken from these sources:

- `e_ident[EI_CLASS]` determines whether header width follows the `ELF32` or `ELF64` layout.
- `e_ident[EI_DATA]` determines whether multi-byte integer fields are read little-endian or big-endian.
- `e_entry` and `e_phoff` widen from the class-specific word size to the package's stable `uint64` surface.
- the program-header table offset comes from `e_phoff`.
- the program-header entry size and count come from `e_phentsize` and `e_phnum`.
- in `ELF32`, `p_flags` appears later in the program-header entry than `p_type`.
- in `ELF64`, `p_flags` appears immediately after `p_type`.

How this repository currently uses the source:

- `Elf.header` tokenizes just the ELF container fields needed to locate the program-header table.
- `Elf.programHeaderAt` follows `e_phoff` and `e_phentsize` to read one indexed program-header entry.
- `Elf.file` is only a thin convenience layer over `Elf.header` plus `Elf.programHeaderAt`.
- ELF tests cover both class widths, both endian encodings, indexed table lookup, and rejection of unsupported `EI_DATA` values.

Keep new ELF work tied back to the generic ABI structure definitions rather than inferring header layouts casually.
