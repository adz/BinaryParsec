namespace BinaryParsec.Protocols.Modbus

open System
open System.IO
open BinaryParsec

/// Stable Modbus TCP parse entry points for application code.
[<AbstractClass; Sealed>]
type ModbusTcp private () =
    static member private ParseFrameCore(input: ReadOnlySpan<byte>) : ParseResult<ModbusTcpFrame> =
        match Contiguous.run ModbusTcpParser.frame input with
        | Error error -> Error error
        | Ok slice ->
            let pduBytes = ByteSlice.asSpan input slice.Pdu

            match Contiguous.run ModbusPduParser.pdu pduBytes with
            | Error error ->
                Error
                    { Position = ParsePosition.create (slice.Pdu.Offset + error.Position.ByteOffset) error.Position.BitOffset
                      Message = error.Message }
            | Ok pduSlice ->
                match ModbusPduParser.materialize "Modbus TCP" pduBytes slice.Pdu.Offset pduSlice with
                | Error error -> Error error
                | Ok pdu ->
                    Ok
                        { TransactionId = slice.TransactionId
                          UnitId = slice.UnitId
                          Pdu = pdu }

    /// Parses one Modbus TCP frame and returns a result-oriented model for F# callers.
    static member TryParseFrame(input: ReadOnlySpan<byte>) : ParseResult<ModbusTcpFrame> =
        ModbusTcp.ParseFrameCore input

    /// Parses one Modbus TCP frame and returns a result-oriented model for array callers.
    static member TryParseFrame(input: byte array) : ParseResult<ModbusTcpFrame> =
        if isNull input then
            nullArg (nameof input)

        let inputSpan = ReadOnlySpan<byte>(input)
        ModbusTcp.ParseFrameCore inputSpan

    /// Parses one Modbus TCP frame into out parameters for C# callers.
    static member TryParseFrame(input: ReadOnlySpan<byte>, frame: byref<ModbusTcpFrame>, error: byref<ParseError>) : bool =
        match ModbusTcp.ParseFrameCore input with
        | Ok parsed ->
            frame <- parsed
            error <- Unchecked.defaultof<ParseError>
            true
        | Error parseError ->
            frame <- Unchecked.defaultof<ModbusTcpFrame>
            error <- parseError
            false

    /// Parses one Modbus TCP frame into out parameters for array callers.
    static member TryParseFrame(input: byte array, frame: byref<ModbusTcpFrame>, error: byref<ParseError>) : bool =
        if isNull input then
            nullArg (nameof input)

        let inputSpan = ReadOnlySpan<byte>(input)
        ModbusTcp.TryParseFrame(inputSpan, &frame, &error)

    /// Parses one Modbus TCP frame or raises `InvalidDataException` when the input is invalid.
    static member ParseFrame(input: ReadOnlySpan<byte>) : ModbusTcpFrame =
        match ModbusTcp.ParseFrameCore input with
        | Ok frame -> frame
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn

    /// Parses one Modbus TCP frame or raises `InvalidDataException` when the input is invalid.
    static member ParseFrame(input: byte array) : ModbusTcpFrame =
        if isNull input then
            nullArg (nameof input)

        let inputSpan = ReadOnlySpan<byte>(input)
        ModbusTcp.ParseFrame inputSpan
