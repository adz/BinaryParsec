namespace BinaryParsec.Protocols.Protobuf

open BinaryParsec

/// The zero-copy payload for one supported Protocol Buffers wire field.
type ProtobufFieldValueSlice =
    | Varint of uint64
    | LengthDelimited of ByteSlice
