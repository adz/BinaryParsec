namespace BinaryParsec.Protocols.Modbus

open System
open BinaryParsec

/// Low-level Modbus RTU parsing over the shared contiguous-input core.
[<RequireQualifiedAccess>]
module internal ModbusRtuParser =
    let internal incompleteFrameMessage =
        "Modbus RTU frame must contain address, PDU, and CRC."

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
                    let pduLength = remaining - 3

                    match Contiguous.``byte``.Invoke(input, position) with
                    | Error error -> Error error
                    | Ok(struct (address, afterAddress)) ->
                        // RTU ADU layout:
                        //   address : 1 byte
                        //   PDU     : N bytes
                        //   CRC     : 2 bytes, little-endian on the wire
                        let frameBytes = ByteSlice.create position.ByteOffset (remaining - 2)
                        let pdu = ByteSlice.create afterAddress.ByteOffset pduLength

                        match Contiguous.u16leAt input (ParsePosition.create (afterAddress.ByteOffset + pduLength) 0) with
                        | Error error -> Error error
                        | Ok(struct (expected, nextPosition)) ->
                            let actual = computeCrc input frameBytes

                            Ok(
                                struct (
                                    { Address = address
                                      Pdu = pdu
                                      Crc =
                                        { Expected = expected
                                          Actual = actual
                                          IsMatch = expected = actual } },
                                    nextPosition
                                )
                            ))
