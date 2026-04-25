namespace BinaryParsec.Protocols.Can

open System
open BinaryParsec

[<RequireQualifiedAccess>]
module internal CanClassicMaterializer =
    let private trailingBytesMessage =
        "CAN classic frame must end immediately after the controller payload bytes."

    let materializeFrame (input: ReadOnlySpan<byte>) (slice: CanClassicFrameSlice) endPosition =
        if slice.IsExtendedFrame then
            Contiguous.failAt (ParsePosition.create 1 4) CanClassicParser.invalidExtendedFrameMessage
        elif endPosition.ByteOffset <> input.Length || endPosition.BitOffset <> 0 then
            Contiguous.failAt endPosition trailingBytesMessage
        else
            let payload = ByteSlice.asSpan input slice.Payload

            Ok
                { BaseIdentifier = slice.BaseIdentifier
                  IsRemoteTransmissionRequest = slice.IsRemoteTransmissionRequest
                  DataLengthCode = slice.DataLengthCode
                  Data = payload.ToArray() }
