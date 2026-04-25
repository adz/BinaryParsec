namespace BinaryParsec.Protocols.Modbus

open BinaryParsec
open BinaryParsec.Syntax

/// Low-level Modbus TCP MBAP parsing over the shared contiguous-input core.
[<RequireQualifiedAccess>]
module internal ModbusTcpParser =
    let frame : ContiguousParser<ModbusTcpFrameSlice> =
        parse {
            // MBAP layout:
            //   transaction id : 2 bytes, big-endian
            //   protocol id    : 2 bytes, must be 0 for Modbus
            //   length         : 2 bytes, unit id + PDU bytes
            //   unit id        : 1 byte
            //   PDU            : N bytes
            let! transactionId = u16be

            let! protocolIdPosition = position
            let! protocolId = u16be

            if protocolId <> 0us then
                return! fail protocolIdPosition $"Modbus TCP protocol identifier must be 0, got 0x{protocolId:X4}."
            else
                let! lengthPosition = position
                let! length = u16be

                if length < 2us then
                    return! fail lengthPosition "Modbus TCP MBAP length must include the unit identifier and at least one PDU function code byte."
                else
                    let! remaining = remainingBytes

                    if remaining <> int length then
                        return! fail lengthPosition "Modbus TCP MBAP length must match the remaining unit identifier plus PDU bytes."
                    else
                        let! unitId = ``byte``
                        let! pdu = takeRemaining

                        return
                            { TransactionId = transactionId
                              UnitId = unitId
                              Pdu = pdu }
        }
