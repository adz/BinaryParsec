---
slug: /model/api
title: API hubs
sidebar_position: 1
---

# API hubs

These pages are curated landing pages for the public API families.

They do not try to mirror namespaces one-to-one. Instead, they answer the questions a reader usually has first: what the family is for, what can be done with it, which member groups matter most, and which detail pages should come next.

## Source-aware build inputs

The generated API pipeline should consume the staged assembly and XML docs under `artifacts/api-docs/`.

For the core library, a successful build stages:

- `artifacts/api-docs/BinaryParsec/BinaryParsec.dll`
- `artifacts/api-docs/BinaryParsec/BinaryParsec.xml`

Keep generated API output separate from handwritten reference material in this folder tree.

## Read next

- [Core reading patterns](../core-reading-patterns.md)
- [Modbus package reference](../modbus-package.md)
- [PNG package reference](../png-package.md)
- [Protocol Buffers package reference](../protobuf-package.md)
