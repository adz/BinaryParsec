# Build A Snippet Parser

Use a snippet parser when a binary format needs one narrow reading pattern and a full package would be premature.

The examples below stay on the core `BinaryParsec` surface. They are meant to be the smallest realistic starting points for new snippet milestones.

## Read packed flags from a byte-aligned header

Use `Contiguous.bit` and `Contiguous.bits` when a format stores several fields inside one packed byte or word.

```fsharp
open BinaryParsec

type Header =
    {
        Identifier: uint16
        IsExtended: bool
        LengthCode: byte
    }

let header =
    Contiguous.parse {
        let! identifier = Contiguous.bits 11
        let! _reserved = Contiguous.bit
        let! isExtended = Contiguous.bit
        let! _tail = Contiguous.bits 3
        let! _reservedLow = Contiguous.bit
        let! _rtr = Contiguous.bit
        let! _padding = Contiguous.bits 2
        let! lengthCode = Contiguous.bits 4

        return
            {
                Identifier = uint16 identifier
                IsExtended = isExtended
                LengthCode = byte lengthCode
            }
    }
```

Keep byte-oriented reads out of the middle of a packed-bit sequence unless you have returned to byte alignment.

## Read a varint and its payload bytes

Use `Contiguous.varUInt64` for the integer and `Contiguous.takeVarintPrefixed` for the common length-delimited payload pattern.

```fsharp
open BinaryParsec

type Field =
    {
        Number: uint32
        Payload: ByteSlice
    }

let field =
    Contiguous.parse {
        let! tag = Contiguous.varUInt64
        let number = uint32 (tag >>> 3)
        let! payload = Contiguous.takeVarintPrefixed

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

let deflatePrelude =
    Contiguous.parse {
        let! isFinal = Contiguous.bitsLsbFirst 1
        let! blockType = Contiguous.bitsLsbFirst 2
        let! literalCodeCount = Contiguous.bitsLsbFirst 5

        return isFinal = 1u, blockType, literalCodeCount + 257u
    }
```

Do not try to emulate this with `Contiguous.bits` plus manual shifting. The core has separate primitives because the ordering difference is semantic, not incidental.

## Follow an absolute offset to a dependent table

Use `Contiguous.readAt` when one part of the binary layout stores an absolute byte offset to another structure.

```fsharp
open BinaryParsec

let header =
    Contiguous.parse {
        let! tableOffset = Contiguous.u32le
        let! firstEntry =
            Contiguous.readAt (int tableOffset) (
                Contiguous.parse {
                    let! entryKind = Contiguous.u16le
                    let! entryFlags = Contiguous.u16le
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

type Payload =
    {
        FunctionCode: byte
        Data: ByteSlice
    }

let payload =
    Contiguous.parse {
        let! functionCode = Contiguous.``byte``
        let! data = Contiguous.takeRemainingMinus 0

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

let rec events runningStatus acc =
    Contiguous.parse {
        let! remaining = Contiguous.remainingBytes

        if remaining = 0 then
            return List.rev acc
        else
            let! next = Contiguous.peekByte

            let! status =
                if next >= 0x80uy then
                    Contiguous.``byte`` |> Contiguous.map ValueSome
                else
                    Contiguous.result runningStatus

            match status with
            | ValueNone ->
                let! position = Contiguous.position
                return! Contiguous.failAt position "Running status requires a previous event."
            | ValueSome currentStatus ->
                let! data1 = Contiguous.``byte``
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
