---
title: 'Set Commitlint Line Limits to 200'
type: 'chore'
created: '2026-08-16'
status: 'done'
route: 'one-shot'
---

# Set Commitlint Line Limits to 200

## Intent

**Problem:** Commitlint enforced a 150-character header cap while leaving body line lengths unrestricted, causing commit validation inconsistencies across header, footer, and body lines.

**Approach:** Update `commitlint.config.mjs` to set explicit 200-character line length limits for `header-max-length`, `body-max-line-length`, and `footer-max-line-length`.

## Suggested Review Order

**Commitlint Configuration**

- Configured header, body, and footer maximum line length rules to 200.
  [`commitlint.config.mjs:5`](../../commitlint.config.mjs#L5)
