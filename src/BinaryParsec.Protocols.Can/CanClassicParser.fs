namespace BinaryParsec.Protocols.Can

open BinaryParsec
open BinaryParsec.Syntax

[<RequireQualifiedAccess>]
module internal CanClassicParser =
    let internal invalidExtendedFrameMessage =
        "CAN classic package supports only base-format controller frames; the extended-frame marker is not supported."

    let internal invalidDlcMessage dlc =
        $"CAN classic DLC must be between 0 and 8, got {dlc}."

    let frame =
        parse {
            do! requireByteAligned
            let! baseIdentifier = bits 11
            let! _reserved = bit
            let! isExtendedFrame = bit
            let! _sidlTail = bits 3
            let! _reservedHigh = bit
            let! isRemoteTransmissionRequest = bit
            let! _reservedLow = bits 2
            let! dlcPosition = position
            let! dlc = bits 4

            if dlc > 8u then
                return! fail dlcPosition (invalidDlcMessage dlc)
            else
                let payloadLength = if isRemoteTransmissionRequest then 0 else int dlc

                // Controller-buffer layout:
                //   SIDH/SIDL : packed 11-bit base identifier plus EXIDE marker
                //   DLC byte  : reserved bit, RTR bit, reserved bits, DLC nibble
                //   DATA      : present only for non-remote classic frames
                let! payload = takeSlice payloadLength

                return
                    { BaseIdentifier = uint16 baseIdentifier
                      IsExtendedFrame = isExtendedFrame
                      IsRemoteTransmissionRequest = isRemoteTransmissionRequest
                      DataLengthCode = uint8 dlc
                      Payload = payload }
        }
