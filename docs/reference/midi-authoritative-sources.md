# MIDI Authoritative Sources

The current MIDI package scope is intentionally narrow, but it is still driven by the standard MIDI event model rather than by ad hoc examples.

The package behavior should be checked against:

- the MIDI 1.0 channel voice message definitions
- the standard MIDI variable-length quantity rules used for delta times
- the running-status rules for channel messages in Standard MIDI Files and event streams

The current package uses those sources only for:

- delta-time VLQ width and accumulation rules
- the distinction between channel voice status bytes and system status bytes
- the data-width difference between `Note On` and `Program Change`
- running-status reuse across consecutive channel events

The package does not yet aim to cover:

- full Standard MIDI File chunk parsing
- meta events
- system exclusive events
- system common or system real-time messages
- tempo maps, tracks, or file-level timing interpretation

If the package grows beyond the current narrow channel-event scope, the next work should pull the relevant MIDI file and event references into a more concrete fixture and documentation set at the same time.
