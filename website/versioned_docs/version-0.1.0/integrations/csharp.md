---
title: C# callers
sidebar_position: 2
---

# C# callers

BinaryParsec can be used from C# without forcing the core parser surface to become C#-shaped.

## What this shows

The C#-friendly Modbus, PNG, ELF, CAN, MIDI, and Protocol Buffers facades exist at the package layer. They keep the core parser engine F#-first while still giving C# callers straightforward `TryParse` and throwing entry points.

## What can you do

- Call `TryParseFrame`, `TryParseFile`, or `TryParseMessage` from ordinary C# code.
- Use `out` parameters where a success/failure boolean fits better than a discriminated union.
- Keep the span-based overloads when your input is already contiguous in memory.

## Core shape

The core still exposes `ContiguousParser<'T>`, `ParseResult<'T>`, and the `BinaryParsec.Syntax` computation-expression surface. The package facades translate that model into stable class or module entry points where the caller experience benefits from it.

## Read next

- [Modbus package reference](../reference/modbus-package.md)
- [PNG package reference](../reference/png-package.md)
- [C# interop explanation](../explanation/CSHARP_INTEROP.md)

