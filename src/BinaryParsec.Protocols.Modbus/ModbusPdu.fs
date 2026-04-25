namespace BinaryParsec.Protocols.Modbus

open System

/// Represents one owned Modbus protocol data unit after transport framing is removed.
type ModbusPdu =
    {
        RawFunctionCode: byte
        FunctionCode: byte
        Payload: byte array
        IsExceptionResponse: bool
        ExceptionCode: Nullable<byte>
    }
