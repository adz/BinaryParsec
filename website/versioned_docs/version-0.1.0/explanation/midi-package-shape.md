# MIDI Package Shape

`BinaryParsec.Protocols.Midi` should stay small until a real file-level or live-stream consumer forces broader scope.

The current package is intentionally split into two layers:

- event tokenization and running-status resolution over the contiguous core
- later interpretation into a stable owned event model

That split matters because the immediate parsing pressure is structural:

- delta-time variable-length quantities
- status-byte reuse through running status
- differing data widths across channel event kinds

Those mechanics are useful and reviewable on their own. They should not be buried inside a larger speculative MIDI model before the repository has a concrete need for full file parsing or live transport handling.

The current package deliberately stops at a narrow event subset:

- `Note On`
- `Program Change`

That boundary is intentional. It keeps the package aligned with the snippet that originally pressured parser state while avoiding premature design around:

- Standard MIDI File chunk structure
- track-level semantics
- meta and system-exclusive events
- live transport timing behavior
- incremental execution backends

The point of this package is not to pretend MIDI is finished. The point is to preserve the proven event-state logic in a package boundary without forcing the core into a larger backend or parser abstraction decision early.
