# Modbus Package Shape

`BinaryParsec.Protocols.Modbus` now follows the package rules with three deliberate layers.

## 1. Transport tokenization

`ModbusRtuParser.frame` and `ModbusTcpParser.frame` stop at transport boundaries.

- RTU tokenization reads address, raw PDU bytes, and CRC bytes.
- TCP tokenization reads the MBAP header, validates transport-only fields, and then cuts out the PDU bytes.
- Both tokenizers keep zero-copy slices and avoid folding transport validation into later PDU interpretation.

That split keeps frame-boundary logic readable and makes the shared Modbus payload rules reusable across transports.

## 2. Shared PDU processing

`ModbusPduParser` is transport-agnostic on purpose.

- It reads the Modbus function code plus trailing payload bytes.
- It normalizes exception responses by clearing bit 7 from the logical function code.
- It validates that exception PDUs carry exactly one exception code byte.

This is the point where the package moves from byte boundaries into Modbus semantics.

## 3. Stable package-facing facades

The package-facing `ModbusRtu` and `ModbusTcp` APIs expose owned models and simple parse entry points for F# and C# callers.

- `ModbusRtu` keeps the existing RTU-oriented surface with CRC validation.
- `ModbusTcp` adds the corresponding MBAP-oriented surface without leaking core parser details.
- The C#-friendly overloads stay entirely in the protocol package layer rather than in `BinaryParsec`.

## Why This Shape Matters

- It keeps the core parser engine free of Modbus-specific convenience APIs.
- It gives the package one shared PDU implementation instead of separate RTU and TCP payload logic.
- It makes review easier because RTU and TCP layout comments now sit exactly where transport boundaries are established.
- It aligns tests and docs with the real package boundary instead of leaving Modbus TCP as only a snippet milestone.
