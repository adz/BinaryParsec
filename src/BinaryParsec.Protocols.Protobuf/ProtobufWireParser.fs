namespace BinaryParsec.Protocols.Protobuf

open System
open BinaryParsec

[<RequireQualifiedAccess>]
module internal ProtobufWireParser =
    let internal invalidFieldNumberZeroMessage =
        "Protocol Buffers field number 0 is invalid."

    let internal unsupportedWireTypeMessage wireType =
        $"Protocol Buffers wire type %d{wireType} is not supported by this package."

    let private maxFieldNumber = 0x1FFFFFFFu

    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let private fieldTag =
        Contiguous.parse {
            let! tagPosition = Contiguous.position
            let! rawTag = Contiguous.varUInt64

            if rawTag > uint64 UInt32.MaxValue then
                return! failAt tagPosition "Protocol Buffers field tag exceeds the supported 32-bit wire range."
            else
                let tag = uint32 rawTag
                let fieldNumber = tag >>> 3
                let wireType = int (tag &&& 0x7u)

                if fieldNumber = 0u then
                    return! failAt tagPosition invalidFieldNumberZeroMessage
                elif fieldNumber > maxFieldNumber then
                    return! failAt tagPosition "Protocol Buffers field number exceeds the supported 29-bit range."
                else
                    match enum<ProtobufWireType> wireType with
                    | ProtobufWireType.Varint ->
                        return
                            { Number = fieldNumber
                              WireType = ProtobufWireType.Varint }
                    | ProtobufWireType.LengthDelimited ->
                        return
                            { Number = fieldNumber
                              WireType = ProtobufWireType.LengthDelimited }
                    | _ ->
                        return! failAt tagPosition (unsupportedWireTypeMessage wireType)
        }

    let field =
        Contiguous.parse {
            let! tag = fieldTag

            match tag.WireType with
            | ProtobufWireType.Varint ->
                let! value = Contiguous.varUInt64

                return
                    ({ Tag = tag
                       Value = ProtobufFieldValueSlice.Varint value }: ProtobufFieldSlice)
            | ProtobufWireType.LengthDelimited ->
                let! payload = Contiguous.takeVarintPrefixed

                return
                    ({ Tag = tag
                       Value = ProtobufFieldValueSlice.LengthDelimited payload }: ProtobufFieldSlice)
            | _ ->
                return! failAt ParsePosition.origin "Unsupported wire types are rejected while reading the field tag."
        }
