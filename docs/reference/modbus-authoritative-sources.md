# Modbus Authoritative Sources

This page records the external Modbus sources that drive the package implementation and tests.

The package currently follows two primary documents:

## Application protocol

- `Modbus Application Protocol Specification V1.1b3`
- Publisher: Modbus Organization
- Date: April 26, 2012

Rules taken from this source:

- the Modbus PDU starts with one function code byte
- exception responses set bit 7 on the transmitted function code
- exception responses carry one exception code byte as the PDU payload
- Modbus TCP uses an MBAP header with transaction identifier, protocol identifier, length, unit identifier, and then the PDU
- the MBAP protocol identifier is `0x0000` for Modbus
- the MBAP length counts the unit identifier plus PDU bytes

## Serial line transport

- `Modbus over Serial Line Specification and Implementation Guide V1.02`
- Publisher: Modbus Organization
- Date: December 20, 2006

Rules taken from this source:

- a Modbus RTU frame carries address, PDU, and CRC
- the RTU CRC covers the address and PDU bytes
- the RTU CRC is transmitted little-endian on the wire

## How This Repository Uses The Sources

- transport tokenizers implement the RTU and MBAP layout rules directly
- shared PDU processing implements the common function-code and exception-response rules once
- facade tests use representative RTU and TCP frames that exercise those documented constraints

Keep new Modbus work tied back to these sources rather than inferring transport or PDU behavior casually.
