namespace BinaryParsec.Protocols.Modbus

open System
open System.IO
open BinaryParsec

/// Stable Modbus RTU parse entry points for application code.
[<AbstractClass; Sealed>]
type ModbusRtu private () =
    static member private ParseFrameCore(input: ReadOnlySpan<byte>) : ParseResult<ModbusRtuFrame> =
        match Contiguous.run ModbusRtuParser.frame input with
        | Error error -> Error error
        | Ok slice when not slice.Crc.IsMatch ->
            Error
                { Position = ParsePosition.create (input.Length - 2) 0
                  Message = ModbusRtuParser.crcMismatchMessage slice.Crc.Expected slice.Crc.Actual }
        | Ok slice ->
            let pduBytes = ByteSlice.asSpan input slice.Pdu

            match Contiguous.run ModbusPduParser.pdu pduBytes with
            | Error error ->
                Error
                    { Position = ParsePosition.create (slice.Pdu.Offset + error.Position.ByteOffset) error.Position.BitOffset
                      Message = error.Message }
            | Ok pduSlice ->
                match ModbusPduParser.materialize "Modbus RTU" pduBytes slice.Pdu.Offset pduSlice with
                | Error error -> Error error
                | Ok pdu ->
                    Ok
                        { Address = slice.Address
                          RawFunctionCode = pdu.RawFunctionCode
                          FunctionCode = pdu.FunctionCode
                          Payload = pdu.Payload
                          IsExceptionResponse = pdu.IsExceptionResponse
                          ExceptionCode = pdu.ExceptionCode
                          Crc = slice.Crc }

    /// Parses one Modbus RTU frame and returns a result-oriented model for F# callers.
    static member TryParseFrame(input: ReadOnlySpan<byte>) : ParseResult<ModbusRtuFrame> =
        ModbusRtu.ParseFrameCore input

    /// Parses one Modbus RTU frame and returns a result-oriented model for array callers.
    static member TryParseFrame(input: byte array) : ParseResult<ModbusRtuFrame> =
        if isNull input then
            nullArg (nameof input)

        let inputSpan = ReadOnlySpan<byte>(input)
        ModbusRtu.ParseFrameCore(inputSpan)

    /// Parses one Modbus RTU frame into out parameters for C# callers.
    static member TryParseFrame(input: ReadOnlySpan<byte>, frame: byref<ModbusRtuFrame>, error: byref<ParseError>) : bool =
        match ModbusRtu.ParseFrameCore input with
        | Ok parsed ->
            frame <- parsed
            error <- Unchecked.defaultof<ParseError>
            true
        | Error parseError ->
            frame <- Unchecked.defaultof<ModbusRtuFrame>
            error <- parseError
            false

    /// Parses one Modbus RTU frame into out parameters for array callers.
    static member TryParseFrame(input: byte array, frame: byref<ModbusRtuFrame>, error: byref<ParseError>) : bool =
        if isNull input then
            nullArg (nameof input)

        let inputSpan = ReadOnlySpan<byte>(input)
        ModbusRtu.TryParseFrame(inputSpan, &frame, &error)

    /// Parses one Modbus RTU frame or raises `InvalidDataException` when the input is invalid.
    static member ParseFrame(input: ReadOnlySpan<byte>) : ModbusRtuFrame =
        match ModbusRtu.ParseFrameCore input with
        | Ok frame -> frame
        | Error error ->
            let exn = InvalidDataException(error.Message)
            exn.Data["ParsePosition"] <- error.Position
            raise exn

    /// Parses one Modbus RTU frame or raises `InvalidDataException` when the input is invalid.
    static member ParseFrame(input: byte array) : ModbusRtuFrame =
        if isNull input then
            nullArg (nameof input)

        let inputSpan = ReadOnlySpan<byte>(input)
        ModbusRtu.ParseFrame(inputSpan)
