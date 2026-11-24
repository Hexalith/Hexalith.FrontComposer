---
title: Localize inline command form controls
type: bugfix
created: 2026-11-24
status: done
route: oneshot
review_loop_iteration: 0
baseline_commit: ed60b9843f26aa08fd6125ec424b9cf0f20b930e
context: []
---

<frozen-after-approval reason="DW-1130 from the deferred work ledger">

## Intent

Given an inline command form in French, render the Cancel button and its accessible name as Annuler and use a French command form name around the adopter’s command label. Preserve the English control and form text and use English fallback copy when the shell resource is unavailable.

</frozen-after-approval>

## Implementation Notes


The renderer uses one localized Cancel value for the visible and accessible labels. The form uses the existing formatted resource fallback around its command label.

## Review Triage Log

- Fixed the unavailable and whitespace-only resource handling and used French nonbreaking spacing.
- The Send prefix is assigned explicitly by a generator test fixture. The production command transform humanizes the type name and supplies no Send prefix.
- The bUnit test exercises both resources. The existing form fallback and the generated renderer snapshots pin the English fallback values.

## Verification

Shell renderer tests passed all 15 cases. SourceTools command form and renderer emitter tests passed all 106 cases.
