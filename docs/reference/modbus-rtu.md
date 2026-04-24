# Modbus RTU Package Reference

`BinaryParsec.Protocols.Modbus` exposes the first package-quality protocol surface in the repository.

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
  Throws `InvalidDataException` on invalid input.
- `ModbusRtu.ParseFrame(byte[])`
  Convenience overload for array callers.

## Public models

### `ModbusRtuFrame`

- `Address`
  The transmitted RTU unit/slave address.
- `RawFunctionCode`
  The transmitted function byte.
- `FunctionCode`
  The logical function code with the exception-response bit removed.
- `Payload`
  The bytes after the function code, copied into owned storage.
- `IsExceptionResponse`
  `true` when the transmitted function code set the exception bit.
- `ExceptionCode`
  Populated only for exception responses.
- `Crc`
  The expected and computed CRC values for the successfully validated frame.

### `ModbusRtuCrcResult`

- `Expected`
  The transmitted CRC from the frame.
- `Actual`
  The CRC computed from the parsed frame bytes.
- `IsMatch`
  Whether the frame passed CRC validation.
