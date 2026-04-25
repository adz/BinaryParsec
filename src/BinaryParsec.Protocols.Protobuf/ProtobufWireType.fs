namespace BinaryParsec.Protocols.Protobuf

/// The supported Protocol Buffers wire types in the current package surface.
type ProtobufWireType =
    | Varint = 0
    | LengthDelimited = 2
