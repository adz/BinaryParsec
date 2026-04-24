namespace BinaryParsec.Protocols.Modbus

open System
open BinaryParsec

/// Low-level Modbus RTU parsing over the shared contiguous-input core.
[<RequireQualifiedAccess>]
module internal ModbusRtuParser =
    let internal incompleteFrameMessage =
        "Modbus RTU frame must contain address, function code, and CRC."

    let internal malformedExceptionPayloadMessage =
        "Modbus RTU exception response payload must contain exactly one exception code byte."

    let internal crcMismatchMessage expected actual =
        $"Modbus RTU CRC mismatch. Expected 0x{expected:X4}, computed 0x{actual:X4}."

    let private minimumFrameLength = 4

    let private computeCrc (input: ReadOnlySpan<byte>) (slice: ByteSlice) =
        let mutable crc = 0xFFFFus
        let bytes = ByteSlice.asSpan input slice

        for index = 0 to bytes.Length - 1 do
            crc <- crc ^^^ uint16 bytes[index]

            for _ = 0 to 7 do
                if (crc &&& 0x0001us) = 0x0001us then
                    crc <- (crc >>> 1) ^^^ 0xA001us
                else
                    crc <- crc >>> 1

        crc

    /// Parses one contiguous Modbus RTU frame and reports the transmitted versus computed CRC.
    let frame =
        ContiguousParser<ModbusRtuFrameSlice>(fun input position ->
            if position.BitOffset <> 0 then
                Contiguous.failAt position "Byte-aligned primitive cannot run when the cursor is at a bit offset."
            else
                let remaining = input.Length - position.ByteOffset

                if remaining < minimumFrameLength then
                    Contiguous.failAt position incompleteFrameMessage
                else
                    let payloadLength = remaining - minimumFrameLength
                    let crcOffset = position.ByteOffset + 2 + payloadLength

                    let frameBytes = ByteSlice.create position.ByteOffset (remaining - 2)
                    let payload = ByteSlice.create (position.ByteOffset + 2) payloadLength
                    let expected =
                        uint16 input[crcOffset]
                        ||| (uint16 input[crcOffset + 1] <<< 8)

                    let actual = computeCrc input frameBytes

                    Ok(
                        struct (
                            { Address = input[position.ByteOffset]
                              FunctionCode = input[position.ByteOffset + 1]
                              Payload = payload
                              Crc =
                                { Expected = expected
                                  Actual = actual
                                  IsMatch = expected = actual } },
                            ParsePosition.create input.Length 0
                        )
                    ))
