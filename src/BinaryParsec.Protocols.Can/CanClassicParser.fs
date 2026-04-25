namespace BinaryParsec.Protocols.Can

open BinaryParsec

[<RequireQualifiedAccess>]
module internal CanClassicParser =
    let internal invalidExtendedFrameMessage =
        "CAN classic package supports only base-format controller frames; the extended-frame marker is not supported."

    let internal invalidDlcMessage dlc =
        $"CAN classic DLC must be between 0 and 8, got {dlc}."

    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let frame =
        Contiguous.parse {
            let! start = Contiguous.position

            if start.BitOffset <> 0 then
                return! failAt start "Byte-aligned primitive cannot run when the cursor is at a bit offset."
            else
                let! baseIdentifier = Contiguous.bits 11
                let! _reserved = Contiguous.bit
                let! isExtendedFrame = Contiguous.bit
                let! _sidlTail = Contiguous.bits 3
                let! _reservedHigh = Contiguous.bit
                let! isRemoteTransmissionRequest = Contiguous.bit
                let! _reservedLow = Contiguous.bits 2
                let! dlcPosition = Contiguous.position
                let! dlc = Contiguous.bits 4

                if dlc > 8u then
                    return! failAt dlcPosition (invalidDlcMessage dlc)
                else
                    let payloadLength = if isRemoteTransmissionRequest then 0 else int dlc

                    // Controller-buffer layout:
                    //   SIDH/SIDL : packed 11-bit base identifier plus EXIDE marker
                    //   DLC byte  : reserved bit, RTR bit, reserved bits, DLC nibble
                    //   DATA      : present only for non-remote classic frames
                    let! payload = Contiguous.take payloadLength

                    return
                        { BaseIdentifier = uint16 baseIdentifier
                          IsExtendedFrame = isExtendedFrame
                          IsRemoteTransmissionRequest = isRemoteTransmissionRequest
                          DataLengthCode = byte dlc
                          Payload = payload }
        }
