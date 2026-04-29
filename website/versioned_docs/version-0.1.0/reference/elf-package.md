---
slug: /model/elf
title: ELF package reference
sidebar_position: 5
---

# ELF package reference

`BinaryParsec.Protocols.Elf` exposes ELF header tokenization plus indexed program-header lookup over the contiguous core.

## What this shows

The package treats the ELF header as a locator for later table lookups. That keeps the file structure visible and keeps table interpretation separate from the first pass over the file header.

## Core shape

- `Elf`
- `ElfFileHeader`
- `ElfProgramHeader`
- `ElfFile`

## What you can do

- Parse ELF32 and ELF64 headers with either endianness.
- Locate program-header table entries by index.
- Materialize the first program header as a stable owned model.
- Keep loader policy and ABI semantics outside the parser boundary.

## Member map

### Create and run

- `Elf.header`
- `Elf.programHeaderAt`
- `Elf.file`
- `Elf.tryParseHeader`
- `Elf.parseHeader`
- `Elf.tryParseFile`
- `Elf.parseFile`

### Offset-based reads

- `readAt`
- `ParsePosition`

### Bridges

- `ElfParser`

## Read next

- [Parse an ELF header and program header](../how-to/parse-elf-header-and-program-header.md)
- [ELF package shape explanation](../explanation/elf-package-shape.md)
- [ELF authoritative sources](elf-authoritative-sources.md)

## Source links

- [Elf.fs](/source/src/BinaryParsec.Protocols.Elf/Elf.fs)
- [ElfParser.fs](/source/src/BinaryParsec.Protocols.Elf/ElfParser.fs)
- [ElfFileHeader.fs](/source/src/BinaryParsec.Protocols.Elf/ElfFileHeader.fs)
