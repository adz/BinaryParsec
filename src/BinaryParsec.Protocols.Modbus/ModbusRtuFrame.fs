namespace BinaryParsec.Protocols.Modbus

open System

/// Represents one validated Modbus RTU frame as a stable owned model.
type ModbusRtuFrame =
    {
        Address: byte
        RawFunctionCode: byte
        FunctionCode: byte
        Payload: byte array
        IsExceptionResponse: bool
        ExceptionCode: Nullable<byte>
        Crc: ModbusRtuCrcResult
    }
