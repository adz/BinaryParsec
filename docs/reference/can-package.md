# CAN Package Reference

`BinaryParsec.Protocols.Can` currently exposes classic controller-frame tokenization plus a validated owned frame parser for base-format CAN frames.

## Public entry points

- `CanClassic.frame`
  Parses one classic controller frame into a zero-copy `CanClassicFrameSlice`.
- `CanClassic.tryParseFrame(ReadOnlySpan<byte>)`
  Returns `ParseResult<CanClassicFrame>`.
- `CanClassic.parseFrame(ReadOnlySpan<byte>)`
  Throws `InvalidDataException` on invalid classic CAN input.

## Public models

### `CanClassicFrameSlice`

- `BaseIdentifier`
  The parsed 11-bit base CAN identifier.
- `IsExtendedFrame`
  Whether the packed controller header marked the frame as extended format.
- `IsRemoteTransmissionRequest`
  Whether the packed header marked the frame as an RTR frame.
- `DataLengthCode`
  The classic CAN DLC nibble.
- `Payload`
  The zero-copy payload slice, or an empty slice for remote frames.

### `CanClassicFrame`

- `BaseIdentifier`
  The parsed 11-bit base CAN identifier.
- `IsRemoteTransmissionRequest`
  Whether the frame is an RTR frame.
- `DataLengthCode`
  The classic CAN DLC nibble.
- `Data`
  The owned payload bytes. This is empty for remote frames.
