---
title: Validation strategy
sidebar_position: 2
---

# Validation strategy

The repo uses a small set of focused tests to keep the intended parser behavior honest:

- zero-allocation checks for hot primitives and representative package paths
- package tests that pin boundary errors to a concrete offset
- snippet tests that mirror the examples in the docs

## What this shows

The allocation-sensitive tests are there to protect the reading path the docs recommend, not to turn the repo into a benchmark suite.

## What can you do

- Re-run the zero-allocation tests when the parser composition layer changes.
- Keep package tests close to the documented example inputs.
- Add new example inputs when a parser family grows a new boundary case.

## Read next

- [BinaryParsec tests](/source/tests/BinaryParsec.Tests/BinaryParsec.Tests.fsproj)
- [Zero-allocation hot path tests](/source/tests/BinaryParsec.Tests/ZeroAllocationHotPathTests.fs)
