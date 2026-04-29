namespace BinaryParsec.Protocols.Modbus

open System
open BinaryParsec
open BinaryParsec.Syntax

/// Shared Modbus PDU parsing that stays transport-agnostic across RTU and TCP.
[<RequireQualifiedAccess>]
module internal ModbusPduParser =
    let internal malformedExceptionPayloadMessage transportName =
        $"{transportName} exception response payload must contain exactly one exception code byte."

    let pdu : ContiguousParser<ModbusPduSlice> =
        ``byte`` .>>. takeRemaining
        |>> fun (fc, p) -> { RawFunctionCode = fc; Payload = p }

    let materialize transportName (input: ReadOnlySpan<byte>) (offset: int) (slice: ModbusPduSlice) : ParseResult<ModbusPdu> =
        let isExceptionResponse = (slice.RawFunctionCode &&& 0x80uy) = 0x80uy

        let functionCode =
            if isExceptionResponse then
                slice.RawFunctionCode &&& 0x7Fuy
            else
                slice.RawFunctionCode

        let payload = (ByteSlice.asSpan input slice.Payload).ToArray()

        if isExceptionResponse && payload.Length <> 1 then
            Error
                { Position = ParsePosition.create (offset + slice.Payload.Offset) 0
                  Message = malformedExceptionPayloadMessage transportName }
        else
            let exceptionCode =
                if isExceptionResponse then
                    Nullable payload[0]
                else
                    Nullable()

            Ok
                { RawFunctionCode = slice.RawFunctionCode
                  FunctionCode = functionCode
                  Payload = payload
                  IsExceptionResponse = isExceptionResponse
                  ExceptionCode = exceptionCode }
