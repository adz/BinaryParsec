namespace BinaryParsec.Protocols.Protobuf

open System.Runtime.CompilerServices

/// One zero-copy Protocol Buffers wire field over the shared binary core.
///
/// The package keeps field tokenization separate from later message-level
/// processing so callers can inspect tags and payload boundaries before
/// deciding whether to collect or interpret a whole message.
[<Struct; IsReadOnlyAttribute>]
type ProtobufFieldSlice =
    {
        Tag: ProtobufFieldTag
        Value: ProtobufFieldValueSlice
    }
