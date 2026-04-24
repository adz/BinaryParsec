namespace BinaryParsec.Protocols.Modbus

open System.Runtime.CompilerServices

/// Reports the transmitted and computed CRC values for one Modbus RTU frame.
[<Struct; IsReadOnlyAttribute>]
type ModbusRtuCrcResult =
    {
        Expected: uint16
        Actual: uint16
        IsMatch: bool
    }
