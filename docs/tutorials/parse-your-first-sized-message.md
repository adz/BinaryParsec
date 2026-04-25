# Parse Your First Sized Message

This tutorial explains the basic mental model of the core `BinaryParsec` surface.

It starts with one of the simplest real binary layouts:

- the first 4 bytes store the payload length
- the next `N` bytes are the payload

If you are new to the library, this is the place to start before the more package-specific guides.

## The Example Input

Suppose the bytes are:

```text
00 00 00 05 48 65 6C 6C 6F
```

Read as:

- `00 00 00 05`
  The payload length, encoded as a big-endian unsigned 32-bit integer.
- `48 65 6C 6C 6F`
  The payload bytes.

That payload happens to be ASCII `"Hello"`.

## The Core Idea

A `ContiguousParser<'T>` is a value that knows how to read a `'T` from a contiguous block of bytes.

So when you see:

```fsharp
let message = ...
```

that usually means:

```text
"message" is the parser for one message
```

not:

```text
"message" is a parsed message that already exists
```

This repo tends to name parsers after the thing they parse:

- `message`
- `frame`
- `field`
- `header`

That is why the parser name is often not `parseMessage`. Both styles are valid, but the repo usually prefers the shorter noun form inside parser modules.

## First Parser

Here is a minimal parser for the example protocol:

```fsharp
open BinaryParsec
open BinaryParsec.Syntax

let message =
    parse {
        let! size = u32be
        let! payload = takeSlice (int size)
        return payload
    }
```

Read it line by line:

- `parse { ... }`
  Build a parser using the computation-expression syntax.
- `let! size = u32be`
  Read 4 bytes as a big-endian `uint32`.
- `let! payload = takeSlice (int size)`
  Now that the size is known, carve out exactly that many bytes as a zero-copy slice.
- `return payload`
  Return the payload portion.

This is the main parser pattern to remember:

```text
read a field -> use its value to decide the next read
```

## What `ByteSlice` Means

`takeSlice` does not copy bytes into a new array.

It returns a `ByteSlice`, which is a small descriptor:

```fsharp
type ByteSlice =
    {
        Offset: int
        Length: int
    }
```

That means:

- where the bytes start in the original input
- how many bytes belong to that region

For the example input, the payload slice would effectively mean:

```fsharp
{ Offset = 4; Length = 5 }
```

That is a zero-copy view of the payload. The parser keeps pointing into the original input buffer instead of allocating a new payload array.

Think of `ByteSlice` as a bookmark into the input.

## Running The Parser

To use the parser, run it against a `ReadOnlySpan<byte>`:

```fsharp
let input =
    [|
        0x00uy; 0x00uy; 0x00uy; 0x05uy
        0x48uy; 0x65uy; 0x6Cuy; 0x6Cuy; 0x6Fuy
    |]

match Contiguous.run message (ReadOnlySpan<byte>(input)) with
| Ok payloadSlice ->
    let payload = ByteSlice.asSpan (ReadOnlySpan<byte>(input)) payloadSlice
    let bytes = payload.ToArray()
    ()
| Error err ->
    ()
```

What happens here:

- `Contiguous.run` starts the parser at byte `0`
- on success, it returns the parsed value
- in this case, the parsed value is a `ByteSlice`
- `ByteSlice.asSpan` resolves that slice back to real bytes over the original input

## Returning A More Explicit Result

If you want a parser that returns both the length and the payload slice, make that explicit:

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

This is often easier to understand than returning only the payload.

## When To Materialize The Payload

There are two common choices:

- Keep `ByteSlice` in the parser result when you want zero-copy tokenization.
- Convert the slice to `byte[]` later when you want a stable owned model.

There is a third common case:

- Parse the bounded payload immediately when the format reads more clearly that way.

For example:

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

That reads more like a format spec because the parser makes the boundary and the nested structure both explicit.

That distinction is common in this repo:

- slice types point into the original input
- materialized types own their data

## One Important Boundary

The core `Contiguous` runner expects one complete contiguous input buffer.

So if the bytes are arriving from a socket or stream, the usual outer flow is:

1. read the 4-byte length prefix
2. decode the size
3. keep buffering until the full payload is available
4. run the parser over the complete message bytes

The parser handles message structure. The transport layer handles incomplete reads and buffering.

## What To Read Next

- [Build a Snippet Parser](../how-to/build-a-snippet-parser.md)
- [Core Reading Patterns Reference](../reference/core-reading-patterns.md)
