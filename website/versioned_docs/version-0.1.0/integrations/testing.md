---
title: Testing and assertions
sidebar_position: 4
---

# Testing and assertions

BinaryParsec fits naturally into xUnit-style tests and focused assertion libraries such as Unquote.

## What this shows

The repository uses tests to prove the parser shape, the error positions, and the zero-allocation hot paths. That makes the test suite part of the docs story rather than a separate validation layer.

## What can you do

- Use unit tests to pin parser behavior to a specific binary layout.
- Use property-style checks where the format shape benefits from them.
- Keep allocation-sensitive paths under repeated-run assertions.

## Core shape

The tests in this repository lean on `Contiguous.run`, `ParsePosition`, and the package `TryParse` facades. That keeps the assertions close to the public surface readers are expected to use.

## Read next

- [Zero-allocation validation](../measure/validation.md)
- [Example corpus](../explanation/EXAMPLE-CORPUS.md)
- [BinaryParsec tests](/source/tests/BinaryParsec.Tests/BinaryParsec.Tests.fsproj)
