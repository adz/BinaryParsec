# Protocol Buffers Package Reference

`BinaryParsec.Protocols.Protobuf` currently exposes Protocol Buffers wire-field tokenization plus a wire-message collector over the shared contiguous parser core.

## Public entry points

- `ProtobufWire.field`
  Parses one wire field into a zero-copy `ProtobufFieldSlice`.
- `ProtobufWire.message`
  Parses repeated wire fields through end of input into owned `ProtobufField` values.
- `ProtobufWire.tryParseField(ReadOnlySpan<byte>)`
  Returns `ParseResult<ProtobufField>` and requires the input to contain exactly one field.
- `ProtobufWire.parseField(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid single-field input.
- `ProtobufWire.tryParseMessage(ReadOnlySpan<byte>)`
  Returns `ParseResult<ProtobufField array>`.
- `ProtobufWire.parseMessage(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid message input.

## Public models

### `ProtobufWireType`

- `Varint`
  Wire type `0`.
- `LengthDelimited`
  Wire type `2`.

### `ProtobufFieldTag`

- `Number`
  The decoded protobuf field number.
- `WireType`
  The decoded protobuf wire type.

### `ProtobufFieldSlice`

- `Tag`
  The decoded field tag.
- `Value`
  The zero-copy field payload as `ProtobufFieldValueSlice`.

### `ProtobufFieldValueSlice`

- `Varint of uint64`
  The parsed varint payload.
- `LengthDelimited of ByteSlice`
  The zero-copy payload bytes.

### `ProtobufField`

- `Tag`
  The decoded field tag.
- `Value`
  The owned field payload as `ProtobufFieldValue`.

### `ProtobufFieldValue`

- `Varint of uint64`
  The materialized varint payload.
- `LengthDelimited of byte array`
  The copied payload bytes.
