# Protocol Buffers Package Shape

`BinaryParsec.Protocols.Protobuf` promotes the earlier wire-format snippet into a package without turning the core into a schema system.

The package is intentionally split into two layers:

- wire-field tokenization over the contiguous core
- later field collection or schema-specific interpretation outside that tokenizer

That split matters because the current BinaryParsec pressure is in the wire encoding itself:

- varint tags
- field-number extraction
- length-delimited payload boundaries
- repeated field walks through a message body

Those reads are useful on their own for zero-copy inspection. They also keep the parser flow visually close to the wire-format rules rather than mixing in message semantics too early.

The tokenizer should only know how to read:

- a valid field tag with a non-zero field number
- the supported payload encodings for the current package surface
- one complete wire field at a time

The later processing layer can then decide what to do with those parsed fields:

- collect the whole message as owned fields
- ignore unknown field numbers
- interpret known fields into a package-specific or application-specific model

Keeping those concerns separate preserves a clean package boundary. The core stays unchanged, the package remains a thin consumer over existing varint and slice primitives, and higher-level protobuf schema handling can evolve independently if later tasks justify it.

The current package deliberately stops short of becoming a full protobuf runtime. It does not yet:

- decode fixed32 or fixed64 wire fields
- decode deprecated start-group or end-group wire fields
- understand `.proto` schemas or generated message types
- provide reflection, descriptors, or text-format support

That boundary is intentional. The package promotes the wire-format snippet into a real consumer while still keeping BinaryParsec focused on binary tokenization and parsing rather than generated object models.
