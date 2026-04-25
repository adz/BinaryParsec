namespace BinaryParsec.Protocols.Protobuf

/// One owned Protocol Buffers wire field materialized from the original input.
type ProtobufField =
    {
        Tag: ProtobufFieldTag
        Value: ProtobufFieldValue
    }
