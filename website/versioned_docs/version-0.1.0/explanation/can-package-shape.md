# CAN Package Shape

`BinaryParsec.Protocols.Can` is the repository's first compact bitfield-heavy protocol package.

The package is intentionally split into two layers:

- controller-frame tokenization over the contiguous core
- later classic-CAN validation and owned-model materialization

That split matters because the interesting CAN pressure here is in the packed header layout rather than in a large semantic model.

The tokenizer should only know how to read:

- the packed 11-bit base identifier spread across controller header bytes
- the `EXIDE` marker that distinguishes base and extended controller layouts
- the `RTR` flag and classic DLC nibble
- the payload boundary implied by the classic controller frame

Those reads are useful on their own for zero-copy inspection and they keep the parser flow visually close to the compact register layout that originally pressured the core bit primitives.

The later processing layer then applies package rules that should not be mixed into the raw read path:

- reject the extended-frame marker in the current classic-only surface
- enforce whole-frame consumption with no trailing bytes
- copy payload data into a stable owned model for application code

Keeping those concerns separate makes the current package easy to review and extend. It also avoids pretending that controller-byte tokenization and later CAN-family support are the same level of abstraction.

The current package deliberately stops short of becoming a full CAN stack. It does not yet:

- parse extended-format CAN frames
- parse raw destuffed wire-level bitstreams
- handle CAN FD frame variants
- layer higher-level protocol payloads such as CANopen or J1939

That boundary keeps the package aligned with the current roadmap. It promotes the earlier CAN snippet into a real package without widening the core or speculating about later CAN-family features.
