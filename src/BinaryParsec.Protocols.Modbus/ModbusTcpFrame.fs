namespace BinaryParsec.Protocols.Modbus

/// Represents one validated Modbus TCP frame with its MBAP transport header and shared PDU.
type ModbusTcpFrame =
    {
        TransactionId: uint16
        UnitId: byte
        Pdu: ModbusPdu
    }
