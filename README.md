# BinaryParsec

`BinaryParsec` is an F# library for high-performance binary tokenization and parsing.

The project goal is a binary parsing library that is:

- idiomatic and intuitive in F#
- clear to work on for both humans and LLMs
- performance-conscious enough for real binary workloads
- driven by real parsing pressure rather than speculative design
- strong at both binary primitives and reusable protocol packages

The intended shape is:

- `BinaryParsec`
  Core parser engine, binary primitives, and thin combinator layer.
- `BinaryParsec.Protocols.*`
  Reusable protocol parsers and format parsers built on top of the core.
- `docs/`
  Explanation, reference, and planning material that keeps the implementation grounded.

The current repo layout already follows that split:

- `src/BinaryParsec`
  Core cursor model, primitives, combinators, and diagnostics.
- `src/BinaryParsec.Protocols.Can`
  The CAN classic controller-frame package over the core, with packed-header tokenization and a stable owned frame facade.
- `src/BinaryParsec.Protocols.Png`
  The PNG format package over the core, with zero-copy chunk tokenization and validated file-level parsing.
- `src/BinaryParsec.Protocols.Modbus`
  The Modbus RTU and TCP protocol package over the core, with shared PDU processing.
- `src/BinaryParsec.Protocols.Protobuf`
  The Protocol Buffers wire-format package over the core, with wire-field tokenization and a thin field-stream collector.

The architectural bias is a dual-layer design:

- a low-level binary cursor and primitive layer
- a thin parser/computation-expression layer built on top

The core library stays F#-first. C# usability matters most at the `BinaryParsec.Protocols.*` layer.

The current package-completion track now includes PNG, Modbus, CAN, and Protocol Buffers wire-format work while keeping the core boundary intact.

See:

- [Agent Rules](AGENTS.md)
- [Plan](PLAN.md)
- [Documentation Index](docs/README.md)
- [Architecture](docs/explanation/ARCHITECTURE.md)
- [Snippet Milestones And Core Coverage](docs/explanation/snippet-milestones-and-core-coverage.md)
- [Build a Snippet Parser](docs/how-to/build-a-snippet-parser.md)
- [Core Reading Patterns Reference](docs/reference/core-reading-patterns.md)
- [Example Corpus](docs/explanation/EXAMPLE-CORPUS.md)
- [CSharp Interop](docs/explanation/CSHARP_INTEROP.md)
- [Tasks](TASKS.md)

## Development Baseline

- SDK: .NET 10
- Build output: repo-root `artifacts/`
