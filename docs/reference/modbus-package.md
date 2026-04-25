# Modbus Package Reference

`BinaryParsec.Protocols.Modbus` exposes transport-specific entry points over one shared Modbus PDU layer.

## Public entry points

- `ModbusRtu.TryParseFrame(ReadOnlySpan<byte>)`
  Returns `ParseResult<ModbusRtuFrame>`.
- `ModbusRtu.TryParseFrame(byte[])`
  Convenience overload for array callers.
- `ModbusRtu.TryParseFrame(ReadOnlySpan<byte>, out ModbusRtuFrame, out ParseError)`
  C#-friendly success/failure shape.
- `ModbusRtu.TryParseFrame(byte[], out ModbusRtuFrame, out ParseError)`
  Convenience overload for array callers.
- `ModbusRtu.ParseFrame(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid RTU input.
- `ModbusRtu.ParseFrame(byte[])`
  Convenience overload for array callers.
- `ModbusTcp.TryParseFrame(ReadOnlySpan<byte>)`
  Returns `ParseResult<ModbusTcpFrame>`.
- `ModbusTcp.TryParseFrame(byte[])`
  Convenience overload for array callers.
- `ModbusTcp.TryParseFrame(ReadOnlySpan<byte>, out ModbusTcpFrame, out ParseError)`
  C#-friendly success/failure shape.
- `ModbusTcp.TryParseFrame(byte[], out ModbusTcpFrame, out ParseError)`
  Convenience overload for array callers.
- `ModbusTcp.ParseFrame(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid TCP input.
- `ModbusTcp.ParseFrame(byte[])`
  Convenience overload for array callers.

## Public models

### `ModbusPdu`

- `RawFunctionCode`
  The transmitted function code byte.
- `FunctionCode`
  The logical function code with the exception-response bit removed.
- `Payload`
  The PDU bytes after the function code, copied into owned storage.
- `IsExceptionResponse`
  `true` when the transmitted function code set bit 7.
- `ExceptionCode`
  Populated only for exception responses.

### `ModbusRtuFrame`

- `Address`
  The transmitted RTU unit/slave address.
- `RawFunctionCode`
  The transmitted function byte from the PDU.
- `FunctionCode`
  The logical function code with the exception bit removed.
- `Payload`
  The PDU bytes after the function code, copied into owned storage.
- `IsExceptionResponse`
  `true` when the transmitted function code set bit 7.
- `ExceptionCode`
  Populated only for exception responses.
- `Crc`
  The transmitted and computed CRC values for the RTU frame.

### `ModbusTcpFrame`

- `TransactionId`
  The MBAP transaction identifier.
- `UnitId`
  The transmitted TCP unit identifier byte.
- `Pdu`
  The shared Modbus PDU after MBAP transport framing is removed.

### `ModbusRtuCrcResult`

- `Expected`
  The transmitted CRC from the RTU frame.
- `Actual`
  The CRC computed from the RTU address plus PDU bytes.
- `IsMatch`
  Whether the RTU frame passed CRC validation.
