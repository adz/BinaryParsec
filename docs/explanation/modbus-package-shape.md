# Modbus Package Shape

`BinaryParsec.Protocols.Modbus` now has two layers on purpose.

## Stable facade

The package-facing `ModbusRtu` API exposes stable, owned models and parse entry points that are practical from both F# and C#. That is the surface real application code should target first.

The facade rejects invalid CRC values and malformed exception responses because protocol packages should make common misuse harder, not easier.

## Low-level parser path

The package still keeps a low-level zero-copy parser path for tests and advanced scenarios. That layer stays close to the core parser engine and preserves the allocation-free hot path.

Keeping both layers in the protocol package avoids pushing protocol-specific convenience APIs down into `BinaryParsec` itself.
