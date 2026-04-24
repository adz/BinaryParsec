namespace BinaryParsec.Tests

open BinaryParsec

/// Captures one shared Modbus PDU that can sit behind multiple transports.
type ModbusPduSnippet =
    {
        FunctionCode: byte
        Data: ByteSlice
    }

/// Captures one tiny Modbus TCP frame with its MBAP transport header and shared PDU.
type ModbusTcpMbapSnippet =
    {
        TransactionId: uint16
        UnitId: byte
        Payload: ModbusPduSnippet
    }

[<RequireQualifiedAccess>]
module internal ModbusTcpMbapSnippet =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let pdu : ContiguousParser<ModbusPduSnippet> =
        Contiguous.parse {
            let! functionCode = Contiguous.``byte``
            let! data = Contiguous.takeRemainingMinus 0

            return
                {
                    FunctionCode = functionCode
                    Data = data
                }
        }

    /// Parses one Modbus TCP MBAP header, validates its framing fields, and then
    /// reuses the shared transport-agnostic PDU parser for the payload bytes.
    let frame : ContiguousParser<ModbusTcpMbapSnippet> =
        Contiguous.parse {
            let! transactionId = Contiguous.u16be

            let! protocolIdPosition = Contiguous.position
            let! protocolId = Contiguous.u16be

            if protocolId <> 0us then
                return! failAt protocolIdPosition $"Modbus TCP protocol identifier must be 0, got 0x{protocolId:X4}."
            else
                let! lengthPosition = Contiguous.position
                let! length = Contiguous.u16be

                if length < 2us then
                    return! failAt lengthPosition "Modbus TCP MBAP length must include the unit identifier and function code."
                else
                    let! remaining = Contiguous.remainingBytes

                    if remaining <> int length then
                        return! failAt lengthPosition "Modbus TCP MBAP length must match the remaining unit identifier plus PDU bytes."
                    else
                        let! unitId = Contiguous.``byte``
                        let! payload = pdu

                        return
                            {
                                TransactionId = transactionId
                                UnitId = unitId
                                Payload = payload
                            }
        }
