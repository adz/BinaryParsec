namespace BinaryParsec.Tests

open BinaryParsec

/// Captures the packed metadata from a tiny CAN classic controller-style header snippet.
type CanClassicHeaderSnippet =
    {
        BaseIdentifier: uint16
        IsExtendedFrame: bool
        IsRemoteTransmissionRequest: bool
        DataLengthCode: byte
    }

[<RequireQualifiedAccess>]
module internal CanClassicSnippet =
    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let private classicDlc =
        Contiguous.parse {
            let! dlcPosition = Contiguous.position
            let! _reserved = Contiguous.bit
            let! isRemoteRequest = Contiguous.bit
            let! _reservedLow = Contiguous.bits 2
            let! dlc = Contiguous.bits 4

            if dlc > 8u then
                return! failAt dlcPosition $"CAN classic DLC must be between 0 and 8, got {dlc}."
            else
                return struct (isRemoteRequest, byte dlc)
        }

    /// Parses the packed base-ID, EXIDE, RTR, and DLC fields from a tiny CAN classic header snippet.
    ///
    /// The layout follows the common controller-style register packing where the
    /// 11-bit base identifier spans `SIDH` plus the top three bits of `SIDL`,
    /// the next `SIDL` flag bit carries the extended-frame marker, and the DLC
    /// byte packs `RTR` plus the 4-bit CAN classic length code.
    let header =
        Contiguous.parse {
            let! baseIdentifier = Contiguous.bits 11
            let! _reserved = Contiguous.bit
            let! isExtendedFrame = Contiguous.bit
            let! _sidlTail = Contiguous.bits 3
            let! struct (isRemoteTransmissionRequest, dataLengthCode) = classicDlc

            return
                {
                    BaseIdentifier = uint16 baseIdentifier
                    IsExtendedFrame = isExtendedFrame
                    IsRemoteTransmissionRequest = isRemoteTransmissionRequest
                    DataLengthCode = dataLengthCode
                }
        }
