namespace BinaryParsec.Protocols.Can

open System.Runtime.CompilerServices
open BinaryParsec

/// Represents one classic CAN controller-frame tokenization result over the shared binary core.
///
/// The package keeps this slice zero-copy so callers can inspect the compact
/// header fields and payload boundaries before choosing whether to materialize
/// an owned frame model.
[<Struct; IsReadOnlyAttribute>]
type CanClassicFrameSlice =
    {
        BaseIdentifier: uint16
        IsExtendedFrame: bool
        IsRemoteTransmissionRequest: bool
        DataLengthCode: byte
        Payload: ByteSlice
    }
