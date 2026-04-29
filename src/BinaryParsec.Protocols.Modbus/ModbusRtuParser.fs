namespace BinaryParsec.Protocols.Modbus

open System
open BinaryParsec
open BinaryParsec.Syntax

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
            match Contiguous.requireByteAligned.Invoke(input, position) with
            | Error error -> Error error
            | Ok struct ((), _) ->
                match Contiguous.position.Invoke(input, position) with
                | Error error -> Error error
                | Ok struct (start, _) ->
                    match Contiguous.remainingBytes.Invoke(input, position) with
                    | Error error -> Error error
                    | Ok struct (rem, _) ->
                        if rem < minimumFrameLength then
                            Contiguous.failAt start incompleteFrameMessage
                        else
                            match Contiguous.``byte``.Invoke(input, position) with
                            | Error error -> Error error
                            | Ok struct (address, afterAddress) ->
                                let pduLength = rem - 3

                                match Contiguous.takeAt pduLength input afterAddress with
                                | Error error -> Error error
                                | Ok struct (pdu, afterPdu) ->
                                    match Contiguous.u16leAt input afterPdu with
                                    | Error error -> Error error
                                    | Ok struct (expectedCrc, nextPosition) ->
                                        let frameBytes = ByteSlice.create start.ByteOffset (rem - 2)
                                        let actualCrc = computeCrc input frameBytes

                                        Ok
                                            struct (
                                                { Address = address
                                                  Pdu = pdu
                                                  Crc =
                                                    { Expected = expectedCrc
                                                      Actual = actualCrc
                                                      IsMatch = expectedCrc = actualCrc } },
                                                nextPosition
                                            )
        )
