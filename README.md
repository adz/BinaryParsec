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

The architectural bias is a dual-layer design:

- a low-level binary cursor and primitive layer
- a thin parser/computation-expression layer built on top

The core library stays F#-first. C# usability matters most at the `BinaryParsec.Protocols.*` layer.

See:

- [Agent Rules](AGENTS.md)
- [Plan](PLAN.md)
- [Documentation Index](docs/README.md)
- [Architecture](docs/explanation/ARCHITECTURE.md)
- [Example Corpus](docs/explanation/EXAMPLE-CORPUS.md)
- [CSharp Interop](docs/explanation/CSHARP_INTEROP.md)
- [Tasks](TASKS.md)

## Development Baseline

- SDK: .NET 10
- Build output: repo-root `artifacts/`
