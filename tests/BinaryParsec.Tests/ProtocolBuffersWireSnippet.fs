namespace BinaryParsec.Tests

open BinaryParsec

/// Captures one tiny Protocol Buffers wire-format message with a varint ID and bytes payload.
type ProtocolBuffersWireSnippet =
    {
        Id: uint64
        Payload: ByteSlice
    }

[<RequireQualifiedAccess>]
type internal ProtocolBuffersWireType =
    | Varint = 0
    | LengthDelimited = 2

[<RequireQualifiedAccess>]
module internal ProtocolBuffersWireSnippet =
    type private FieldTag =
        {
            Number: uint32
            WireType: ProtocolBuffersWireType
        }

    type private PartialMessage =
        {
            Id: uint64 voption
            Payload: ByteSlice voption
        }

    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let private fieldTag =
        Contiguous.parse {
            let! tagPosition = Contiguous.position
            let! rawTag = Contiguous.varUInt64

            if rawTag > uint64 System.UInt32.MaxValue then
                return! failAt tagPosition "Protocol Buffers field tag exceeds the supported 32-bit range."
            else
                let tag = uint32 rawTag
                let fieldNumber = tag >>> 3

                if fieldNumber = 0u then
                    return! failAt tagPosition "Protocol Buffers field number 0 is invalid."
                else
                    match enum<ProtocolBuffersWireType> (int (tag &&& 0x7u)) with
                    | ProtocolBuffersWireType.Varint ->
                        return
                            {
                                Number = fieldNumber
                                WireType = ProtocolBuffersWireType.Varint
                            }
                    | ProtocolBuffersWireType.LengthDelimited ->
                        return
                            {
                                Number = fieldNumber
                                WireType = ProtocolBuffersWireType.LengthDelimited
                            }
                    | wireType ->
                        return! failAt tagPosition $"Protocol Buffers wire type %d{int wireType} is not supported by this snippet."
        }

    let private skipField tag =
        match tag.WireType with
        | ProtocolBuffersWireType.Varint ->
            Contiguous.map ignore Contiguous.varUInt64
        | ProtocolBuffersWireType.LengthDelimited ->
            Contiguous.map ignore Contiguous.takeVarintPrefixed
        | _ ->
            invalidOp "Unsupported wire types are rejected while reading the tag."

    let rec private messageFields (state: PartialMessage) : ContiguousParser<ProtocolBuffersWireSnippet> =
        Contiguous.parse {
            let! remaining = Contiguous.remainingBytes

            if remaining = 0 then
                match state with
                | { Id = ValueSome id; Payload = ValueSome payload } ->
                    return
                        ({
                            Id = id
                            Payload = payload
                        }: ProtocolBuffersWireSnippet)
                | _ ->
                    let! endPosition = Contiguous.position
                    return! failAt endPosition "Protocol Buffers snippet requires both field 1 (id) and field 2 (payload)."
            else
                let! tag = fieldTag

                match tag.Number, tag.WireType with
                | 1u, ProtocolBuffersWireType.Varint ->
                    let! id = Contiguous.varUInt64
                    return! messageFields { state with Id = ValueSome id }
                | 2u, ProtocolBuffersWireType.LengthDelimited ->
                    let! payload = Contiguous.takeVarintPrefixed
                    return! messageFields { state with Payload = ValueSome payload }
                | _ ->
                    do! skipField tag
                    return! messageFields state
        }

    /// Parses a tiny Protocol Buffers message that keeps an ID varint and bytes payload while skipping unknown fields.
    let message : ContiguousParser<ProtocolBuffersWireSnippet> =
        messageFields { Id = ValueNone; Payload = ValueNone }
