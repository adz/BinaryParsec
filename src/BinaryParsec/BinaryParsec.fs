namespace BinaryParsec

open System
open System.Runtime.CompilerServices

/// Tracks the current cursor location within contiguous binary input.
///
/// The core parser runner threads this position through every primitive so
/// errors, byte reads, and bit reads all report against the same state model.
[<Struct; IsReadOnlyAttribute>]
type ParsePosition =
    {
        ByteOffset: int
        BitOffset: int
    }

/// Describes a contiguous region of the original input without copying bytes.
///
/// Higher-level protocol and format parsers return slices to keep the core
/// runner span-based while still exposing payload boundaries to callers.
[<Struct; IsReadOnlyAttribute>]
type ByteSlice =
    {
        Offset: int
        Length: int
    }

/// Carries an offset-aware parse failure from the contiguous-input core.
type ParseError =
    {
        Position: ParsePosition
        Message: string
    }

/// Represents the success or failure of a top-level parse.
type ParseResult<'T> = Result<'T, ParseError>

/// Represents a parser that reads from contiguous binary input and advances a shared cursor.
///
/// The success path carries its value and next position in a struct tuple so
/// repeated span-based reads can stay allocation-free.
type ContiguousParser<'T> = delegate of ReadOnlySpan<byte> * ParsePosition -> ParseResult<struct ('T * ParsePosition)>

/// Helpers for creating and validating binary cursor positions.
[<RequireQualifiedAccess>]
module ParsePosition =
    /// The start of contiguous input at byte 0, bit 0.
    let origin = { ByteOffset = 0; BitOffset = 0 }

    /// Creates a validated cursor position within contiguous input.
    let create byteOffset bitOffset =
        if byteOffset < 0 then
            invalidArg (nameof byteOffset) "Byte offset must be non-negative."

        if bitOffset < 0 || bitOffset > 7 then
            invalidArg (nameof bitOffset) "Bit offset must be between 0 and 7."

        { ByteOffset = byteOffset; BitOffset = bitOffset }

/// Helpers for constructing zero-copy byte ranges over contiguous input.
[<RequireQualifiedAccess>]
module ByteSlice =
    /// Creates a validated zero-copy slice descriptor.
    let create offset length =
        if offset < 0 then
            invalidArg (nameof offset) "Slice offset must be non-negative."

        if length < 0 then
            invalidArg (nameof length) "Slice length must be non-negative."

        { Offset = offset; Length = length }

    /// Resolves a slice descriptor back to a span over the original input.
    let asSpan (input: ReadOnlySpan<byte>) (slice: ByteSlice) : ReadOnlySpan<byte> =
        input.Slice(slice.Offset, slice.Length)

/// Primitive reads and minimal composition over the contiguous binary parser core.
///
/// This module is the thin layer between the cursor-based runner and
/// higher-level format or protocol parsers.
[<RequireQualifiedAccess>]
module Contiguous =
    let private readFailure position bytesRequested =
        Error
            {
                Position = position
                Message = $"Unexpected end of input while reading {bytesRequested} byte(s)."
            }

    let private readBitFailure position bitsRequested =
        Error
            {
                Position = position
                Message = $"Unexpected end of input while reading {bitsRequested} bit(s)."
            }

    let private requireByteAlignedAt position =
        if position.BitOffset = 0 then
            Ok()
        else
            Error
                {
                    Position = position
                    Message = "Byte-aligned primitive cannot run when the cursor is at a bit offset."
                }

    let private requireBytes bytesRequested (input: ReadOnlySpan<byte>) position =
        let remaining = input.Length - position.ByteOffset

        if remaining >= bytesRequested then
            Ok()
        else
            readFailure position bytesRequested

    let private requireBits bitsRequested (input: ReadOnlySpan<byte>) position =
        let remainingBits = ((input.Length - position.ByteOffset) * 8) - position.BitOffset

        if remainingBits >= bitsRequested then
            Ok()
        else
            readBitFailure position bitsRequested

    let private advanceBytes count position =
        { ByteOffset = position.ByteOffset + count
          BitOffset = 0 }

    let private advanceBits count position =
        let totalBits = position.BitOffset + count

        { ByteOffset = position.ByteOffset + (totalBits / 8)
          BitOffset = totalBits % 8 }

    /// Returns a successful parser result without advancing the cursor.
    let succeed value position : ParseResult<struct ('T * ParsePosition)> =
        Ok(struct (value, position))

    /// Provides the computation-expression surface over the low-level contiguous parser shape.
    type ContiguousParserBuilder() =
        member _.Bind(parser: ContiguousParser<'T>, binder: 'T -> ContiguousParser<'U>) : ContiguousParser<'U> =
            ContiguousParser<'U>(fun input position ->
                match parser.Invoke(input, position) with
                | Ok(struct (value, nextPosition)) ->
                    let nextParser = binder value
                    nextParser.Invoke(input, nextPosition)
                | Error error -> Error error)

        member _.Return(value: 'T) : ContiguousParser<'T> =
            ContiguousParser<'T>(fun _ position -> Ok(struct (value, position)))

        member _.ReturnFrom(parser: ContiguousParser<'T>) : ContiguousParser<'T> =
            parser

        member _.BindReturn(parser: ContiguousParser<'T>, mapping: 'T -> 'U) : ContiguousParser<'U> =
            ContiguousParser<'U>(fun input position ->
                match parser.Invoke(input, position) with
                | Ok(struct (value, nextPosition)) -> Ok(struct (mapping value, nextPosition))
                | Error error -> Error error)

        member _.MergeSources(left: ContiguousParser<'T>, right: ContiguousParser<'U>) : ContiguousParser<struct ('T * 'U)> =
            ContiguousParser<struct ('T * 'U)>(fun input position ->
                match left.Invoke(input, position) with
                | Ok(struct (leftValue, afterLeft)) ->
                    match right.Invoke(input, afterLeft) with
                    | Ok(struct (rightValue, afterRight)) -> Ok(struct (struct (leftValue, rightValue), afterRight))
                    | Error error -> Error error
                | Error error -> Error error)

        member _.Zero() : ContiguousParser<unit> =
            ContiguousParser<unit>(fun _ position -> Ok(struct ((), position)))

    /// Computation-expression builder for contiguous parsers.
    let parse = ContiguousParserBuilder()

    /// Transforms a parser result without changing how much input is consumed.
    let map mapping (source: ContiguousParser<'T>) =
        ContiguousParser<'U>(fun input position ->
            match source.Invoke(input, position) with
            | Ok(struct (value, nextPosition)) -> succeed (mapping value) nextPosition
            | Error error -> Error error)

    /// Sequences two fixed-shape parsers and maps their values without intermediate allocations.
    let map2 mapping (left: ContiguousParser<'T>) (right: ContiguousParser<'U>) =
        ContiguousParser<'V>(fun input position ->
            match left.Invoke(input, position) with
            | Ok(struct (leftValue, afterLeft)) ->
                match right.Invoke(input, afterLeft) with
                | Ok(struct (rightValue, afterRight)) -> succeed (mapping leftValue rightValue) afterRight
                | Error error -> Error error
            | Error error -> Error error)

    /// Sequences two fixed-shape parsers and returns their values in a struct tuple.
    let mergeSources left right =
        map2 (fun leftValue rightValue -> struct (leftValue, rightValue)) left right

    /// Chooses the next parser from the previous parsed value.
    let bind (binder: 'T -> ContiguousParser<'U>) (source: ContiguousParser<'T>) =
        ContiguousParser<'U>(fun input position ->
            match source.Invoke(input, position) with
            | Ok(struct (value, nextPosition)) ->
                let nextParser = binder value
                nextParser.Invoke(input, nextPosition)
            | Error error -> Error error)

    /// Runs a parser at an absolute byte offset without changing the current cursor.
    let readAt byteOffset (parser: ContiguousParser<'T>) =
        if byteOffset < 0 then
            invalidArg (nameof byteOffset) "Read offset must be non-negative."

        ContiguousParser<'T>(fun input position ->
            let targetPosition = ParsePosition.create byteOffset 0

            match parser.Invoke(input, targetPosition) with
            | Ok(struct (value, _)) -> succeed value position
            | Error error -> Error error)

    /// Sequences two parsers and returns both parsed values.
    let zip left right =
        map2 (fun leftValue rightValue -> leftValue, rightValue) left right

    /// Runs two parsers and keeps the value produced by the right parser.
    let keepRight left right =
        map2 (fun _ rightValue -> rightValue) left right

    /// Runs two parsers and keeps the value produced by the left parser.
    let keepLeft left right =
        map2 (fun leftValue _ -> leftValue) left right

    let internal takeAt count (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes count input position with
            | Error error -> Error error
            | Ok() ->
                let slice = ByteSlice.create position.ByteOffset count
                succeed slice (advanceBytes count position)

    let internal u32beAt (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes 4 input position with
            | Error error -> Error error
            | Ok() ->
                let start = position.ByteOffset
                let value =
                    (uint32 input[start] <<< 24)
                    ||| (uint32 input[start + 1] <<< 16)
                    ||| (uint32 input[start + 2] <<< 8)
                    ||| uint32 input[start + 3]

                succeed value (advanceBytes 4 position)

    let internal u32leAt (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes 4 input position with
            | Error error -> Error error
            | Ok() ->
                let start = position.ByteOffset
                let value =
                    uint32 input[start]
                    ||| (uint32 input[start + 1] <<< 8)
                    ||| (uint32 input[start + 2] <<< 16)
                    ||| (uint32 input[start + 3] <<< 24)

                succeed value (advanceBytes 4 position)

    let internal u16leAt (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes 2 input position with
            | Error error -> Error error
            | Ok() ->
                let start = position.ByteOffset
                let value =
                    uint16 input[start]
                    ||| (uint16 input[start + 1] <<< 8)

                succeed value (advanceBytes 2 position)

    let internal u64beAt (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes 8 input position with
            | Error error -> Error error
            | Ok() ->
                let start = position.ByteOffset
                let value =
                    (uint64 input[start] <<< 56)
                    ||| (uint64 input[start + 1] <<< 48)
                    ||| (uint64 input[start + 2] <<< 40)
                    ||| (uint64 input[start + 3] <<< 32)
                    ||| (uint64 input[start + 4] <<< 24)
                    ||| (uint64 input[start + 5] <<< 16)
                    ||| (uint64 input[start + 6] <<< 8)
                    ||| uint64 input[start + 7]

                succeed value (advanceBytes 8 position)

    let internal u64leAt (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            match requireBytes 8 input position with
            | Error error -> Error error
            | Ok() ->
                let start = position.ByteOffset
                let value =
                    uint64 input[start]
                    ||| (uint64 input[start + 1] <<< 8)
                    ||| (uint64 input[start + 2] <<< 16)
                    ||| (uint64 input[start + 3] <<< 24)
                    ||| (uint64 input[start + 4] <<< 32)
                    ||| (uint64 input[start + 5] <<< 40)
                    ||| (uint64 input[start + 6] <<< 48)
                    ||| (uint64 input[start + 7] <<< 56)

                succeed value (advanceBytes 8 position)

    let internal varUInt64At (input: ReadOnlySpan<byte>) position =
        match requireByteAlignedAt position with
        | Error error -> Error error
        | Ok() ->
            let mutable nextPosition = position
            let mutable shift = 0
            let mutable count = 0
            let mutable value = 0UL
            let mutable result = Unchecked.defaultof<ParseResult<struct (uint64 * ParsePosition)>>
            let mutable finished = false

            while not finished do
                match requireBytes 1 input nextPosition with
                | Error error ->
                    result <- Error error
                    finished <- true
                | Ok() ->
                    let current = input[nextPosition.ByteOffset]
                    let payload = uint64 (current &&& 0x7Fuy)
                    let hasContinuation = (current &&& 0x80uy) <> 0uy

                    if count = 9 && (hasContinuation || payload > 1UL) then
                        result <-
                            Error
                                {
                                    Position = nextPosition
                                    Message = "Varint exceeds 64 bits."
                                }
                        finished <- true
                    else
                        value <- value ||| (payload <<< shift)
                        nextPosition <- advanceBytes 1 nextPosition
                        count <- count + 1

                        if not hasContinuation then
                            result <- succeed value nextPosition
                            finished <- true
                        else
                            shift <- shift + 7

            result

    /// Chooses between multiple parsers, returning the result of the first successful one.
    let choice (parsers: ContiguousParser<'T> seq) =
        ContiguousParser<'T>(fun input position ->
            let mutable result = Error { Position = position; Message = "Choice sequence was empty." }
            let mutable finished = false
            use enumerator = parsers.GetEnumerator()

            while not finished && enumerator.MoveNext() do
                match enumerator.Current.Invoke(input, position) with
                | Ok struct (value, nextPosition) ->
                    result <- Ok struct (value, nextPosition)
                    finished <- true
                | Error error -> result <- Error error

            result)

    /// Attempts to run a parser, returning None if it fails.
    let optional (parser: ContiguousParser<'T>) =
        ContiguousParser<'T option>(fun input position ->
            match parser.Invoke(input, position) with
            | Ok struct (value, nextPosition) -> succeed (Some value) nextPosition
            | Error _ -> succeed None position)

    /// Runs a parser zero or more times until it fails.
    let many (parser: ContiguousParser<'T>) =
        ContiguousParser<'T list>(fun input position ->
            let mutable nextPosition = position
            let results = ResizeArray<'T>()
            let mutable finished = false

            while not finished do
                match parser.Invoke(input, nextPosition) with
                | Ok struct (value, after) ->
                    results.Add(value)
                    nextPosition <- after
                | Error _ -> finished <- true

            succeed (List.ofSeq results) nextPosition)

    /// Runs a parser one or more times until it fails.
    let many1 (parser: ContiguousParser<'T>) =
        ContiguousParser<'T list>(fun input position ->
            match parser.Invoke(input, position) with
            | Error error -> Error error
            | Ok struct (first, afterFirst) ->
                let mutable nextPosition = afterFirst
                let results = ResizeArray<'T>()
                results.Add(first)
                let mutable finished = false

                while not finished do
                    match parser.Invoke(input, nextPosition) with
                    | Ok struct (value, after) ->
                        results.Add(value)
                        nextPosition <- after
                    | Error _ -> finished <- true

                succeed (List.ofSeq results) nextPosition)

    /// Parses a sequence of items separated by a separator parser.
    let sepBy (item: ContiguousParser<'T>) (separator: ContiguousParser<'U>) =
        ContiguousParser<'T list>(fun input position ->
            match item.Invoke(input, position) with
            | Error _ -> succeed [] position
            | Ok struct (first, afterFirst) ->
                let mutable nextPosition = afterFirst
                let results = ResizeArray<'T>()
                results.Add(first)
                let mutable finished = false

                while not finished do
                    match separator.Invoke(input, nextPosition) with
                    | Ok struct (_, afterSep) ->
                        match item.Invoke(input, afterSep) with
                        | Ok struct (value, afterItem) ->
                            results.Add(value)
                            nextPosition <- afterItem
                        | Error _ -> finished <- true
                    | Error _ -> finished <- true

                succeed (List.ofSeq results) nextPosition)

    /// Parses one or more items separated by a separator parser.
    let sepBy1 (item: ContiguousParser<'T>) (separator: ContiguousParser<'U>) =
        parse {
            let! first = item
            let! rest = many (keepRight separator item)
            return first :: rest
        }

    /// Runs a parser and requires it to be surrounded by open and close parsers.
    let between (openParser: ContiguousParser<'O>) (closeParser: ContiguousParser<'C>) (item: ContiguousParser<'T>) =
        parse {
            let! _ = openParser
            let! value = item
            let! _ = closeParser
            return value
        }

    /// Attaches a label to a parser to improve error messages upon failure.
    let label label (parser: ContiguousParser<'T>) =
        ContiguousParser<'T>(fun input position ->
            match parser.Invoke(input, position) with
            | Ok struct (value, nextPosition) -> Ok struct (value, nextPosition)
            | Error error ->
                Error
                    { error with
                        Message = $"%s{label}: %s{error.Message}" })

    /// Runs a parser from an explicit starting position.
    let runWith (parser: ContiguousParser<'T>) (input: ReadOnlySpan<byte>) (position: ParsePosition) : ParseResult<'T> =
        match parser.Invoke(input, position) with
        | Ok(struct (value, _)) -> Ok value
        | Error error -> Error error

    /// Runs a parser from the origin and returns only the parsed value.
    let run (parser: ContiguousParser<'T>) (input: ReadOnlySpan<byte>) : ParseResult<'T> =
        match parser.Invoke(input, ParsePosition.origin) with
        | Ok(struct (value, _)) -> Ok value
        | Error error -> Error error

    /// Runs a parser from the origin and requires it to consume the full input.
    let runExact (parser: ContiguousParser<'T>) (input: ReadOnlySpan<byte>) : ParseResult<'T> =
        match parser.Invoke(input, ParsePosition.origin) with
        | Ok(struct (value, nextPosition)) when nextPosition.ByteOffset = input.Length && nextPosition.BitOffset = 0 ->
            Ok value
        | Ok(struct (_, nextPosition)) ->
            Error
                {
                    Position = nextPosition
                    Message = "Parser did not consume the full input."
                }
        | Error error -> Error error

    /// Builds a failure at an explicit cursor position.
    let failAt position message : ParseResult<'T> =
        Error { Position = position; Message = message }

    /// Builds a failing parser at an explicit cursor position.
    let fail position message : ContiguousParser<'T> =
        ContiguousParser<'T>(fun _ _ -> failAt position message)

    /// Lifts a value into a parser that consumes no input.
    let result value =
        ContiguousParser<'T>(fun _ position -> succeed value position)

    /// Returns the current parse position without consuming input.
    let position =
        ContiguousParser<ParsePosition>(fun _ current -> succeed current current)

    /// Fails if the current cursor is not at a byte boundary.
    let requireByteAligned =
        ContiguousParser<unit>(fun _ position ->
            match requireByteAlignedAt position with
            | Error error -> Error error
            | Ok() -> succeed () position)

    /// Returns the remaining byte count from the current position.
    let remainingBytes =
        ContiguousParser<int>(fun input current ->
            succeed (input.Length - current.ByteOffset) current)

    /// Reads one byte and advances to the next byte-aligned position.
    let ``byte`` =
        ContiguousParser<byte>(fun input position ->
            match requireByteAlignedAt position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 1 input position with
                | Error error -> Error error
                | Ok() ->
                    let value = input[position.ByteOffset]
                    succeed value (advanceBytes 1 position))

    /// Reads one byte without advancing the cursor.
    let peekByte =
        ContiguousParser<byte>(fun input position ->
            match requireByteAlignedAt position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 1 input position with
                | Error error -> Error error
                | Ok() -> succeed input[position.ByteOffset] position)

    /// Advances by a byte count without returning any data.
    let skip count =
        if count < 0 then
            invalidArg (nameof count) "Skip count must be non-negative."

        ContiguousParser<unit>(fun input position ->
            match requireByteAlignedAt position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes count input position with
                | Error error -> Error error
                | Ok() -> succeed () (advanceBytes count position))

    /// Returns a zero-copy slice of the next `count` bytes.
    let take count =
        if count < 0 then
            invalidArg (nameof count) "Take count must be non-negative."

        ContiguousParser<ByteSlice>(fun input position -> takeAt count input position)

    /// Matches an exact byte sequence and returns its zero-copy input slice.
    let expectBytes (expected: byte array) mismatchMessage =
        if isNull expected then
            nullArg (nameof expected)

        ContiguousParser<ByteSlice>(fun input position ->
            match takeAt expected.Length input position with
            | Error error -> Error error
            | Ok(struct (slice, nextPosition)) ->
                let actual = ByteSlice.asSpan input slice
                let mutable matches = true
                let mutable index = 0

                while matches && index < expected.Length do
                    if actual[index] <> expected[index] then
                        matches <- false

                    index <- index + 1

                if matches then
                    succeed slice nextPosition
                else
                    failAt position mismatchMessage)

    /// Returns a zero-copy slice that leaves `trailingCount` bytes unread.
    let takeRemainingMinus trailingCount =
        if trailingCount < 0 then
            invalidArg (nameof trailingCount) "Trailing count must be non-negative."

        ContiguousParser<ByteSlice>(fun input position ->
            match requireByteAlignedAt position with
            | Error error -> Error error
            | Ok() ->
                let count = input.Length - position.ByteOffset - trailingCount

                if count < 0 then
                    readFailure position trailingCount
                else
                    let slice = ByteSlice.create position.ByteOffset count
                    succeed slice (advanceBytes count position))

    /// Returns a zero-copy slice of all remaining unread bytes.
    let takeRemaining =
        takeRemainingMinus 0

    /// Reads an unsigned 16-bit integer in big-endian byte order.
    let u16be =
        ContiguousParser<uint16>(fun input position ->
            match requireByteAlignedAt position with
            | Error error -> Error error
            | Ok() ->
                match requireBytes 2 input position with
                | Error error -> Error error
                | Ok() ->
                    let start = position.ByteOffset
                    let value =
                        (uint16 input[start] <<< 8)
                        ||| uint16 input[start + 1]

                    succeed value (advanceBytes 2 position))

    /// Reads an unsigned 16-bit integer in little-endian byte order.
    let u16le =
        ContiguousParser<uint16>(fun input position -> u16leAt input position)

    /// Reads an unsigned 32-bit integer in big-endian byte order.
    let u32be =
        ContiguousParser<uint32>(fun input position -> u32beAt input position)

    /// Reads an unsigned 32-bit integer in little-endian byte order.
    let u32le =
        ContiguousParser<uint32>(fun input position -> u32leAt input position)

    /// Reads an unsigned 64-bit integer in big-endian byte order.
    let u64be =
        ContiguousParser<uint64>(fun input position -> u64beAt input position)

    /// Reads an unsigned 64-bit integer in little-endian byte order.
    let u64le =
        ContiguousParser<uint64>(fun input position -> u64leAt input position)

    /// Reads an unsigned 64-bit varint using the Protocol Buffers wire encoding.
    let varUInt64 =
        ContiguousParser<uint64>(fun input position -> varUInt64At input position)

    /// Reads one bit in most-significant-bit-first order.
    let bit =
        ContiguousParser<bool>(fun input position ->
            match requireBytes 1 input position with
            | Error error -> Error error
            | Ok() ->
                let current = input[position.ByteOffset]
                let shift = 7 - position.BitOffset
                let value = ((current >>> shift) &&& 1uy) = 1uy
                succeed value (advanceBits 1 position))

    /// Reads `count` bits in most-significant-bit-first order into an unsigned integer.
    let bits count =
        if count < 1 || count > 32 then
            invalidArg (nameof count) "Bit count must be between 1 and 32."

        ContiguousParser<uint32>(fun input position ->
            match requireBits count input position with
            | Error error -> Error error
            | Ok() ->
                let mutable remaining = count
                let mutable nextPosition = position
                let mutable value = 0u

                while remaining > 0 do
                    let current = input[nextPosition.ByteOffset]
                    let availableInByte = 8 - nextPosition.BitOffset
                    let takeCount = min remaining availableInByte
                    let shift = availableInByte - takeCount
                    let mask = (1 <<< takeCount) - 1
                    let chunk = uint32 ((int current >>> shift) &&& mask)

                    value <- (value <<< takeCount) ||| chunk
                    nextPosition <- advanceBits takeCount nextPosition
                    remaining <- remaining - takeCount

                succeed value nextPosition)

    /// Reads `count` bits in least-significant-bit-first order into an unsigned integer.
    ///
    /// This matches formats such as DEFLATE that pack low-order bits first
    /// while still advancing through the same contiguous cursor model.
    let bitsLsbFirst count =
        if count < 1 || count > 32 then
            invalidArg (nameof count) "Bit count must be between 1 and 32."

        ContiguousParser<uint32>(fun input position ->
            match requireBits count input position with
            | Error error -> Error error
            | Ok() ->
                let mutable remaining = count
                let mutable nextPosition = position
                let mutable value = 0u
                let mutable bitsRead = 0

                while remaining > 0 do
                    let current = input[nextPosition.ByteOffset]
                    let availableInByte = 8 - nextPosition.BitOffset
                    let takeCount = min remaining availableInByte
                    let mask = (1 <<< takeCount) - 1
                    let chunk = uint32 ((int current >>> nextPosition.BitOffset) &&& mask)

                    value <- value ||| (chunk <<< bitsRead)
                    nextPosition <- advanceBits takeCount nextPosition
                    remaining <- remaining - takeCount
                    bitsRead <- bitsRead + takeCount

                succeed value nextPosition)

    /// Reads a varint length prefix and returns that many bytes as a zero-copy slice.
    let takeVarintPrefixed =
        ContiguousParser<ByteSlice>(fun input position ->
            match varUInt64At input position with
            | Error error -> Error error
            | Ok(struct (length, afterLength)) ->
                if length > uint64 Int32.MaxValue then
                    failAt position "Length-delimited payload exceeds supported contiguous input size."
                else
                    takeAt (int length) input afterLength)

    /// Parses exactly the next `count` bytes as a bounded nested payload.
    let parseExactly count (parser: ContiguousParser<'T>) =
        if count < 0 then
            invalidArg (nameof count) "Bounded parse length must be non-negative."

        ContiguousParser<'T>(fun input position ->
            match takeAt count input position with
            | Error error -> Error error
            | Ok(struct (slice, nextPosition)) ->
                let nestedInput = ByteSlice.asSpan input slice

                match runExact parser nestedInput with
                | Ok value -> succeed value nextPosition
                | Error error ->
                    Error
                        {
                            Position = ParsePosition.create (slice.Offset + error.Position.ByteOffset) error.Position.BitOffset
                            Message = error.Message
                        })

    /// Parses all remaining unread bytes as one bounded nested payload.
    let parseRemaining (parser: ContiguousParser<'T>) =
        ContiguousParser<'T>(fun input position ->
            let remaining = input.Length - position.ByteOffset
            let nested = parseExactly remaining parser
            nested.Invoke(input, position))

/// Preferred low-ceremony surface for writing contiguous binary parsers.
///
/// Open this module in parser files when the goal is to keep the binary layout
/// visually dominant while still using the existing contiguous backend.
module Syntax =
    // --- Backend-Neutral Semantics ---
    // These concepts are expected to survive across execution backends.

    let result = Contiguous.result
    let failAt = Contiguous.failAt
    let fail = Contiguous.fail
    let position = Contiguous.position
    let requireByteAligned = Contiguous.requireByteAligned
    let remainingBytes = Contiguous.remainingBytes
    let map = Contiguous.map
    let map2 = Contiguous.map2
    let bind = Contiguous.bind
    let zip = Contiguous.zip
    let keepLeft = Contiguous.keepLeft
    let keepRight = Contiguous.keepRight
    let parse = Contiguous.parse
    let ``byte`` = Contiguous.``byte``
    let skip = Contiguous.skip
    let expectBytes = Contiguous.expectBytes
    let u16be = Contiguous.u16be
    let u16le = Contiguous.u16le
    let u32be = Contiguous.u32be
    let u32le = Contiguous.u32le
    let u64be = Contiguous.u64be
    let u64le = Contiguous.u64le
    let varUInt64 = Contiguous.varUInt64
    let bit = Contiguous.bit
    let bits = Contiguous.bits
    let bitsLsbFirst = Contiguous.bitsLsbFirst
    let parseExactly = Contiguous.parseExactly
    let parseRemaining = Contiguous.parseRemaining
    let choice = Contiguous.choice
    let optional = Contiguous.optional
    let many = Contiguous.many
    let many1 = Contiguous.many1
    let sepBy = Contiguous.sepBy
    let sepBy1 = Contiguous.sepBy1
    let between = Contiguous.between
    let label = Contiguous.label

    // Operators
    let (>>=) p f = bind f p
    let (<!>) f p = map f p
    let (|>>) p f = map f p
    let (.>>.) p1 p2 = zip p1 p2
    let (.>>) p1 p2 = keepLeft p1 p2
    let (>>.) p1 p2 = keepRight p1 p2
    let (<|>) p1 p2 = choice [p1; p2]
    let (<?>) p l = label l p

    // --- Contiguous-Only Conveniences ---
    // These concepts rely on the presence of a stable, contiguous buffer in memory.

    let peekByte = Contiguous.peekByte
    let takeSlice = Contiguous.take
    let takeRemaining = Contiguous.takeRemaining
    let takeRemainingMinus = Contiguous.takeRemainingMinus
    let takeVarintSlice = Contiguous.takeVarintPrefixed
    let readAt = Contiguous.readAt
