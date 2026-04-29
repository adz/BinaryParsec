# Build A Snippet Parser

Use a snippet parser when a binary format needs one narrow reading pattern and a full package would be premature.

The examples below stay on the core `BinaryParsec` surface. They are meant to be the smallest realistic starting points for new snippet milestones.

If you are still learning the basic mental model of `Contiguous.parse`, parser naming, `Contiguous.run`, or `ByteSlice`, read [Parse Your First Sized Message](../tutorials/parse-your-first-sized-message.md) first.

## Read a fixed-width size prefix and then that many bytes

This is the basic dependent-read pattern:

- read one field
- use its value to decide the next read

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

type SizedMessage =
    {
        Size: uint32
        Payload: ByteSlice
    }

let message =
    parse {
        let! size = u32be
        let! payload = takeSlice (int size)

        return
            {
                Size = size
                Payload = payload
            }
    }
```

`Payload` is a `ByteSlice`, not a copied `byte[]`. It records the offset and length of the payload within the original input so the parser can stay zero-copy.

If the binary format is easier to read as a nested structure than as a deferred slice, use `parseExactly` instead:

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

let payload =
    parse {
        let! command = ``byte``
        let! argument = u16be
        return command, argument
    }

let message =
    parse {
        let! payloadLength = ``byte``
        let! parsedPayload = parseExactly (int payloadLength) payload
        return payloadLength, parsedPayload
    }
```

## Read packed flags from a byte-aligned header

Use `Contiguous.bit` and `Contiguous.bits` when a format stores several fields inside one packed byte or word.

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

type Header =
    {
        Identifier: uint16
        IsExtended: bool
        LengthCode: byte
    }

let header =
    parse {
        let! identifier = bits 11
        let! _reserved = bit
        let! isExtended = bit
        let! _tail = bits 3
        let! _reservedLow = bit
        let! _rtr = bit
        let! _padding = bits 2
        let! lengthCode = bits 4

        return
            {
                Identifier = uint16 identifier
                IsExtended = isExtended
                LengthCode = uint8 lengthCode
            }
    }
```

Keep byte-oriented reads out of the middle of a packed-bit sequence unless you have returned to byte alignment.

## Read a varint and its payload bytes

Use `Contiguous.varUInt64` for the integer and `Contiguous.takeVarintPrefixed` for the common length-delimited payload pattern.

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

type Field =
    {
        Number: uint32
        Payload: ByteSlice
    }

let field =
    parse {
        let! tag = varUInt64
        let number = uint32 (tag >>> 3)
        let! payload = takeVarintSlice

        return
            {
                Number = number
                Payload = payload
            }
    }
```

If the wire format supports unknown fields, keep the skipping logic in the snippet parser rather than trying to make the core guess the policy.

## Read least-significant-bit-first packed fields

Use `Contiguous.bitsLsbFirst` when the format defines bit order from the low bit upward within each byte.

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

let deflatePrelude =
    parse {
        let! isFinal = bitsLsbFirst 1
        let! blockType = bitsLsbFirst 2
        let! literalCodeCount = bitsLsbFirst 5

        return isFinal = 1u, blockType, literalCodeCount + 257u
    }
```

Do not try to emulate this with `Contiguous.bits` plus manual shifting. The core has separate primitives because the ordering difference is semantic, not incidental.

## Follow an absolute offset to a dependent table

Use `Contiguous.readAt` when one part of the binary layout stores an absolute byte offset to another structure.

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

let header =
    parse {
        let! tableOffset = u32le
        let! firstEntry =
            readAt (int tableOffset) (
                parse {
                    let! entryKind = u16le
                    let! entryFlags = u16le
                    return entryKind, entryFlags
                })

        return tableOffset, firstEntry
    }
```

`readAt` restores the original cursor after the nested parser runs. Use it for dependent lookups, not for hidden control flow.

## Reuse one payload parser behind multiple frames

Keep the shared payload parser separate when two transports or envelopes carry the same inner message.

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

type Payload =
    {
        FunctionCode: byte
        Data: ByteSlice
    }

let payload =
    parse {
        let! functionCode = ``byte``
        let! data = takeRemaining

        return
            {
                FunctionCode = functionCode
                Data = data
            }
    }
```

Build the framing parser around that shared payload parser instead of pushing transport-specific helpers down into the core.

## Thread state across a byte stream

Keep stream-local state inside the parser loop when later events depend on earlier bytes.

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

let rec events runningStatus acc =
    parse {
        let! remaining = remainingBytes

        if remaining = 0 then
            return List.rev acc
        else
            let! next = peekByte

            let! status =
                if next >= 0x80uy then
                    ``byte`` |> map ValueSome
                else
                    result runningStatus

            match status with
            | ValueNone ->
                let! currentPosition = position
                return! fail currentPosition "Running status requires a previous event."
            | ValueSome currentStatus ->
                let! data1 = ``byte``
                return! events (ValueSome currentStatus) ((currentStatus, data1) :: acc)
    }
```

This pattern keeps the runner simple and makes stateful format behavior visible in the parser itself.

## When To Stop At A Snippet

A snippet is enough when all of these are true:

- it proves one missing capability family
- it uses the core as a real consumer rather than as an artificial example
- it does not need a stable public package API yet

If the binary format now needs a durable consumer-facing surface, package docs, or C#-friendly entry points, it has moved past snippet scope and should become a proper `BinaryParsec.Protocols.*` package.
