# Contiguous Input Model

This note defines the first intended shape of the contiguous-input backend.

It exists to make the low-level model explicit before the core implementation grows around it. The goal is not to lock every type name in place. The goal is to fix the semantics that later primitives and combinators should depend on.

## Why Start Here

Contiguous input is the first backend because it keeps the core parsing mechanics visible:

- the input is a `ReadOnlySpan<byte>`
- cursor movement is cheap and deterministic
- byte and bit offsets can be reported precisely
- zero-copy slices are possible where the parser semantics allow them

This is the smallest backend that can prove the core architecture without introducing stream buffering, refill rules, or partial-data states too early.

## Backend Scope

The contiguous backend is responsible for:

- holding the original input span
- tracking the current cursor position
- advancing by bytes and bits
- failing with precise offsets
- exposing enough state for low-level primitives

It is not responsible for:

- stream refills
- ownership of copied buffers
- speculative abstraction for non-contiguous sources
- high-level parser composition policy

Those concerns belong in later layers or later backends.

## Intended State Shape

The first useful model is a small state object with the source span and the current logical position.

In F# terms, the intended shape is close to:

```fsharp
type ParsePosition =
    {
        ByteOffset: int
        BitOffset: int
    }

type ContiguousInput =
    {
        Buffer: ReadOnlySpan<byte>
        Position: ParsePosition
    }
```

The exact implementation may use structs, separate fields, or an internal cursor type for performance. The important part is the meaning:

- `Buffer` is the full contiguous input for the current parse run
- `ByteOffset` is the index of the current byte in that original input
- `BitOffset` is the bit position within the current byte

The backend should treat `ParsePosition` as a logical position, not as a slice length or a derived diagnostic string.

## Cursor Invariants

The cursor should follow a small set of rules:

- `ByteOffset` is always between `0` and `Buffer.Length`
- `BitOffset` is always between `0` and `7`
- when `ByteOffset = Buffer.Length`, the only valid `BitOffset` is `0`
- byte-oriented primitives should require byte alignment unless they explicitly document otherwise
- bit-oriented primitives may advance within a byte and across byte boundaries

These invariants keep the backend predictable and make error reporting straightforward.

## Cursor Movement

There are two distinct movement modes.

### Byte-aligned movement

Operations such as `skip 1`, `take 4`, `byte`, `u16be`, and `u16le` should normally run only when `BitOffset = 0`.

Their movement rule is simple:

- consume `n` whole bytes
- increase `ByteOffset` by `n`
- leave `BitOffset` at `0`

If a byte-oriented primitive is invoked when `BitOffset <> 0`, that should be treated as an explicit parser rule violation, not as hidden realignment.

### Bit movement

Bit primitives should advance by a bit count without losing the original byte-relative location.

The intended rule is:

```fsharp
let next byteOffset bitOffset bitsRead =
    let totalBits = bitOffset + bitsRead

    { ByteOffset = byteOffset + (totalBits / 8)
      BitOffset = totalBits % 8 }
```

That keeps the cursor canonical even when a read crosses a byte boundary.

## Byte Offset Semantics

`ByteOffset` should always mean "the zero-based byte index of the current cursor in the original input".

That means:

- it is not the number of remaining bytes
- it is not relative to a temporary slice
- it is stable enough to use in diagnostics
- it can be compared directly with protocol field layouts and format specs

A parser that consumes the first byte successfully moves from byte offset `0` to byte offset `1`.

## Bit Offset Semantics

`BitOffset` should always mean "the zero-based bit index inside the current byte".

The natural interpretation is most-significant-bit first within that byte unless a primitive explicitly documents a different bit ordering. The backend should track the location only. Bit numbering policy for extraction belongs to the bit primitive itself.

Two rules matter here:

- `BitOffset = 0` means the cursor is byte-aligned
- `BitOffset <> 0` means the next byte-oriented primitive must not silently continue

Keeping alignment explicit avoids hidden edge cases in mixed byte/bit parsing.

## Failure Position

A failure should report the position where the parser could not continue, not an earlier checkpoint and not a formatted range unless a higher layer chooses to add one.

The first useful error shape is close to:

```fsharp
type ParseError =
    {
        Position: ParsePosition
        Message: string
    }
```

For the contiguous backend, `Position` should identify:

- the current byte offset
- the current bit offset when a bit parser fails
- the aligned byte position for byte-oriented failures

Examples:

- attempting to read `byte` from an empty input fails at `{ ByteOffset = 0; BitOffset = 0 }`
- reading a second byte from a one-byte input after one successful read fails at `{ ByteOffset = 1; BitOffset = 0 }`
- attempting to read three more bits after consuming six bits from the first byte fails at `{ ByteOffset = 0; BitOffset = 6 }`

This is enough for early diagnostics and for tests that assert exact failure locations.

## Runner Shape

The contiguous runner should stay thin. It should execute a parser against a `ReadOnlySpan<byte>` and return either a value or a `ParseError`.

The current placeholder shape:

```fsharp
type ParseResult<'T> = Result<'T, ParseError>
type SpanParser<'T> = delegate of ReadOnlySpan<byte> -> ParseResult<'T>
```

is acceptable as a temporary shell, but the low-level model should move toward an explicit state transition rather than a parser that only sees the raw input span.

Conceptually, the runner direction is:

```fsharp
type ContiguousParser<'T> = ContiguousInput -> Result<'T * ContiguousInput, ParseError>
```

The exact public shape can differ. The architectural point is that contiguous parsing needs access to cursor state, not just the original input buffer.

## Why This Model Fits BinaryParsec

This model matches the project direction for three reasons:

- it keeps the binary mechanics explicit and testable
- it supports both byte and bit primitives without forcing stream concerns into the core
- it leaves room for a later streaming backend without pretending both backends have identical low-level behavior

That is the right starting point for the next task: replacing the placeholder core with the minimum contiguous-input implementation.
