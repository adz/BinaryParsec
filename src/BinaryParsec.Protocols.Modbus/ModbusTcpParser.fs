namespace BinaryParsec.Protocols.Modbus

open BinaryParsec

/// Low-level Modbus TCP MBAP parsing over the shared contiguous-input core.
[<RequireQualifiedAccess>]
module internal ModbusTcpParser =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let frame : ContiguousParser<ModbusTcpFrameSlice> =
        Contiguous.parse {
            // MBAP layout:
            //   transaction id : 2 bytes, big-endian
            //   protocol id    : 2 bytes, must be 0 for Modbus
            //   length         : 2 bytes, unit id + PDU bytes
            //   unit id        : 1 byte
            //   PDU            : N bytes
            let! transactionId = Contiguous.u16be

            let! protocolIdPosition = Contiguous.position
            let! protocolId = Contiguous.u16be

            if protocolId <> 0us then
                return! failAt protocolIdPosition $"Modbus TCP protocol identifier must be 0, got 0x{protocolId:X4}."
            else
                let! lengthPosition = Contiguous.position
                let! length = Contiguous.u16be

                if length < 2us then
                    return! failAt lengthPosition "Modbus TCP MBAP length must include the unit identifier and at least one PDU function code byte."
                else
                    let! remaining = Contiguous.remainingBytes

                    if remaining <> int length then
                        return! failAt lengthPosition "Modbus TCP MBAP length must match the remaining unit identifier plus PDU bytes."
                    else
                        let! unitId = Contiguous.``byte``
                        let! pdu = Contiguous.takeRemainingMinus 0

                        return
                            { TransactionId = transactionId
                              UnitId = unitId
                              Pdu = pdu }
        }
