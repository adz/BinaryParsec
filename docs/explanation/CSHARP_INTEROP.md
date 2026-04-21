# C# Interop Notes

`BinaryParsec` itself can stay F#-first. The protocol packages should be easy to consume from C#.

## Practical Direction

Keep two layers of API:

- an F#-native parser layer
- a .NET-friendly facade layer for protocol packages

The facade layer should favor shapes such as:

- `TryParse`
- `Parse`
- `ParseFrame`
- `ParseMany`

That means C# users do not need to know about computation expressions, custom operators, or parser combinator conventions.

## Avoid Leaking F#-Only Shapes

For protocol packages, avoid making C# users deal with:

- computation expressions
- curried functions as the main entry point
- deeply nested discriminated unions for routine success paths
- `ref struct`-infected public models unless there is no realistic alternative

Use ordinary .NET-visible result types for the outer API.

## Likely Split

- `BinaryParsec`
  F#-centric and performance-oriented.
- `BinaryParsec.Protocols.*`
  Public entry points tailored for both F# and C# consumers.

## Candidate Public Surface For Protocol Packages

Examples of C#-friendly entry points:

```csharp
var ok = ModbusRtu.TryParseFrame(buffer, out var frame, out var error);
var frame = PngParser.Parse(buffer);
var messages = ProtocolBuffers.ParseMany(buffer);
```

The underlying parser engine can still be implemented in F# with span-oriented internals.

## Important Constraint

Zero-copy span-heavy internals are compatible with C# consumption, but `ref struct` should be contained in the low-level engine wherever possible. The higher-level protocol APIs should return stable .NET types unless a span-based fast path is intentionally exposed.

