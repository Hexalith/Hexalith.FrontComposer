---
title: 'Set commitlint header limit to 150'
type: 'chore'
created: '2026-07-19'
status: 'done'
route: 'one-shot'
---

# Set commitlint header limit to 150

## Intent

**Problem:** Commitlint rejects valid Conventional Commit headers longer than its 100-character default, including the reported 127-character header.

**Approach:** Configure an explicit 150-character header limit while preserving all other Conventional Commit validation rules.

## Suggested Review Order

- The explicit rule accepts descriptive headers through 150 characters and rejects longer ones.
  [`commitlint.config.mjs:11`](../../commitlint.config.mjs#L11)
