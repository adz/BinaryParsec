# C# Interop Notes

`BinaryParsec` stays F#-first. C# usability is confirmed as a protocol-layer concern rather than a core-library concern.

## Confirmed Direction

Keep two API layers:

- `BinaryParsec`
  F#-native parser values, primitives, thin combinators, computation expressions, and low-level diagnostics.
- `BinaryParsec.Protocols.*`
  Protocol-specific entry points that wrap the core with ordinary .NET-facing parse methods.

This means task-oriented protocol APIs can be C#-friendly without pulling C#-specific compromises into the parser engine.

## Facade Contract

Protocol packages should expose a small outer surface built around straightforward parse entry points:

- `TryParse`
- `TryParseFrame`
- `Parse`
- `ParseFrame`
- `ParseMany` where a protocol naturally has repeated messages

Those members should accept standard .NET buffer shapes first:

- `ReadOnlySpan<byte>` for fast-path parsing
- `byte[]` overloads only where they materially improve consumption

They should return stable .NET-visible models or diagnostics:

- readonly structs, records, or classes for successful protocol models
- `bool` plus `out` parameters for `TryParse*`
- exceptions only for `Parse*` methods that intentionally offer an exception-throwing convenience layer

## Core Boundary

Do not push these shapes down into `BinaryParsec` itself:

- static protocol-specific parse methods on the core modules
- C#-oriented overload proliferation
- public APIs that require C# consumers to understand computation expressions
- public models widened only to avoid F# types such as `Result`

The core can continue to expose parser values and low-level data such as `ByteSlice`, `ParseError`, and parser combinators. Protocol packages are the place where those internals are adapted into task-focused entry points.

## Model Guidance

Keep protocol result models stable and unsurprising from C#:

- expose decoded values directly when they are part of the protocol domain
- keep zero-copy slice boundaries when they carry real value for diagnostics or deferred decoding
- avoid leaking `ref struct` constraints through the public protocol surface unless the fast path clearly justifies it

When zero-copy internals need a more stable outer representation, prefer thin wrappers at the protocol package boundary rather than redesigning the core parser types.

## Candidate Protocol Surface

For a protocol package such as `BinaryParsec.Protocols.Modbus`, the intended shape is:

```csharp
var ok = ModbusRtu.TryParseFrame(buffer, out var frame, out var error);
var frame = ModbusRtu.ParseFrame(buffer);
```

The facade should hide parser-combinator mechanics while preserving the same parsing semantics and diagnostics produced by the F# core.
