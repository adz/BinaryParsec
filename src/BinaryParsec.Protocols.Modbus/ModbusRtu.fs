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
            let rawFunctionCode = slice.FunctionCode
            let isExceptionResponse = (rawFunctionCode &&& 0x80uy) = 0x80uy

            let functionCode =
                if isExceptionResponse then
                    rawFunctionCode &&& 0x7Fuy
                else
                    rawFunctionCode

            let payload = (ByteSlice.asSpan input slice.Payload).ToArray()

            if isExceptionResponse && payload.Length <> 1 then
                Error
                    {
                        Position = ParsePosition.create slice.Payload.Offset 0
                        Message = ModbusRtuParser.malformedExceptionPayloadMessage
                    }
            else
                let exceptionCode =
                    if isExceptionResponse then
                        Nullable payload[0]
                    else
                        Nullable()

                Ok
                    { Address = slice.Address
                      RawFunctionCode = rawFunctionCode
                      FunctionCode = functionCode
                      Payload = payload
                      IsExceptionResponse = isExceptionResponse
                      ExceptionCode = exceptionCode
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
