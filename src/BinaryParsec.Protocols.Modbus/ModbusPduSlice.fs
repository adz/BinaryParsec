namespace BinaryParsec.Protocols.Modbus

open System.Runtime.CompilerServices
open BinaryParsec

/// Represents one zero-copy Modbus protocol data unit before transport-specific materialization.
[<Struct; IsReadOnlyAttribute>]
type internal ModbusPduSlice =
    {
        RawFunctionCode: byte
        Payload: ByteSlice
    }
