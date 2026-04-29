# Protocol Buffers Authoritative Sources

This page records the external Protocol Buffers sources that drive the current package implementation and tests.

The current `BinaryParsec.Protocols.Protobuf` package intentionally targets the wire-format layer rather than schema compilation or generated object models.

The primary sources are:

- `Protocol Buffers Encoding`
- Publisher: protobuf.dev / Google
- Scope used here: base-128 varints, field tags, supported wire-type meanings, and length-delimited payload boundaries
- `Protocol Buffers Language Specification`
- Publisher: protobuf.dev / Google
- Scope used here: valid field-number range and the distinction between field numbers, wire types, and higher-level message semantics

Rules currently taken from these sources:

- a field tag is a varint containing the field number in the high bits and the wire type in the low three bits
- field number `0` is invalid
- valid field numbers fit within the protobuf field-number range
- length-delimited fields encode their byte length as a varint prefix
- unsupported wire types should be rejected explicitly rather than guessed at

How this repository currently uses the sources:

- `ProtobufWire.field` tokenizes one complete wire field into a tag plus zero-copy payload boundary
- `ProtobufWire.tryParseMessage` walks repeated fields through end of input and materializes owned field values
- higher-level message interpretation remains outside the wire-field tokenizer so package code does not pretend that wire parsing and schema semantics are the same concern

Keep future protobuf work tied back to these sources rather than inferring wire rules casually.
