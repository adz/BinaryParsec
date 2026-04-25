namespace BinaryParsec.Protocols.Modbus

open System.Runtime.CompilerServices
open BinaryParsec

/// Represents one contiguous Modbus TCP frame before the shared PDU is materialized.
[<Struct; IsReadOnlyAttribute>]
type internal ModbusTcpFrameSlice =
    {
        TransactionId: uint16
        UnitId: byte
        Pdu: ByteSlice
    }
