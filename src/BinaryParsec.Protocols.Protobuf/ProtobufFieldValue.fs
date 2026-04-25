namespace BinaryParsec.Protocols.Protobuf

/// The owned payload for one supported Protocol Buffers wire field.
type ProtobufFieldValue =
    | Varint of uint64
    | LengthDelimited of byte array
