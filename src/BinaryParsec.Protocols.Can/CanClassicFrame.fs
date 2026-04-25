namespace BinaryParsec.Protocols.Can

/// Represents one validated classic CAN frame as a stable owned model.
///
/// The current package targets the common controller-buffer representation for
/// 11-bit identifier frames rather than raw destuffed on-wire CAN bits.
type CanClassicFrame =
    {
        BaseIdentifier: uint16
        IsRemoteTransmissionRequest: bool
        DataLengthCode: byte
        Data: byte array
    }
