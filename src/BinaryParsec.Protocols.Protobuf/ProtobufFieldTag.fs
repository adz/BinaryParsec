namespace BinaryParsec.Protocols.Protobuf

open System.Runtime.CompilerServices

/// The decoded field number and wire type from one Protocol Buffers field tag.
[<Struct; IsReadOnlyAttribute>]
type ProtobufFieldTag =
    {
        Number: uint32
        WireType: ProtobufWireType
    }
