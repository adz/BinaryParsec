namespace BinaryParsec.Protocols.Modbus

open System.Runtime.CompilerServices
open BinaryParsec

/// Represents one contiguous Modbus RTU frame over the shared binary core.
[<Struct; IsReadOnlyAttribute>]
type internal ModbusRtuFrameSlice =
    {
        Address: byte
        Pdu: ByteSlice
        Crc: ModbusRtuCrcResult
    }
