namespace BinaryParsec.Protocols.Protobuf

open System
open BinaryParsec
open BinaryParsec.Syntax

[<RequireQualifiedAccess>]
module internal ProtobufWireParser =
    let internal invalidFieldNumberZeroMessage =
        "Protocol Buffers field number 0 is invalid."

    let internal unsupportedWireTypeMessage wireType =
        $"Protocol Buffers wire type %d{wireType} is not supported by this package."

    let private maxFieldNumber = 0x1FFFFFFFu

    let private fieldTag =
        parse {
            let! tagPosition = position
            let! rawTag = varUInt64

            if rawTag > uint64 UInt32.MaxValue then
                return! fail tagPosition "Protocol Buffers field tag exceeds the supported 32-bit wire range."
            else
                let tag = uint32 rawTag
                let fieldNumber = tag >>> 3
                let wireType = int (tag &&& 0x7u)

                if fieldNumber = 0u then
                    return! fail tagPosition invalidFieldNumberZeroMessage
                elif fieldNumber > maxFieldNumber then
                    return! fail tagPosition "Protocol Buffers field number exceeds the supported 29-bit range."
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
                        return! fail tagPosition (unsupportedWireTypeMessage wireType)
        }

    let field =
        parse {
            let! tag = fieldTag

            match tag.WireType with
            | ProtobufWireType.Varint ->
                let! value = varUInt64

                return
                    ({ Tag = tag
                       Value = ProtobufFieldValueSlice.Varint value }: ProtobufFieldSlice)
            | ProtobufWireType.LengthDelimited ->
                let! payload = takeVarintSlice

                return
                    ({ Tag = tag
                       Value = ProtobufFieldValueSlice.LengthDelimited payload }: ProtobufFieldSlice)
            | _ ->
                return! fail ParsePosition.origin "Unsupported wire types are rejected while reading the field tag."
        }
