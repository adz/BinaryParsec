# CAN Authoritative Sources

This page records the external CAN sources that drive the current package implementation and tests.

The current `BinaryParsec.Protocols.Can` package intentionally targets the compact controller-buffer representation for classic 11-bit CAN frames rather than raw destuffed on-wire bits.

The primary sources are:

- `CAN Specification Version 2.0`
- Publisher: Robert Bosch GmbH
- Scope used here: classic CAN 2.0A base-frame identifier width, remote-frame semantics, and classic DLC range
- `MCP2515 Stand-Alone CAN Controller with SPI Interface`
- Publisher: Microchip Technology
- Scope used here: the common `SIDH`/`SIDL`/`DLC` controller register packing that the package tokenizes

Rules currently taken from these sources:

- a classic base-format CAN identifier is 11 bits wide
- the classic DLC field is limited to values `0` through `8`
- remote transmission request frames carry a requested data length code but no payload bytes
- controller-buffer layouts commonly split the 11-bit base identifier across `SIDH` plus the high bits of `SIDL`
- the controller `EXIDE` marker distinguishes base-format from extended-format frames in the packed header representation

How this repository currently uses the sources:

- `CanClassic.frame` tokenizes the packed controller header and payload boundaries from the `SIDH`/`SIDL`/`DLC` byte layout
- `CanClassic.tryParseFrame` keeps later validation and owned-model materialization separate from that compact tokenization step
- CAN tests cover base-format frame success, remote-frame success, DLC validation, and rejection of the extended-frame marker in the current classic-only package surface

Keep new CAN work tied back to these sources rather than inferring controller-layout details casually.
