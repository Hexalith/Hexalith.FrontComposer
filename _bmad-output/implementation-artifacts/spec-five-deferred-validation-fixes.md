---
title: 'Reject invalid accessibility evidence and skill corpus baselines'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '21298441087977fcc6929e9ce61f3d099d0d290f'
context: []
---

<frozen-after-approval reason="Five locally verifiable items selected from the FrontComposer deferred-work ledger">

## Intent

Close five validation deferrals in FrontComposer. Given an undersized pointer target, require non-overlapping bounds for its equivalent control and check a spacing exception against both supplied neighbors and every other recorded target. Given an AE-04 packet, refuse duplicate target ordinals. Given an AE-09 packet, refuse negative counts or a critical-finding count greater than the total. Given a supplied skill corpus with baseline parse errors, return those errors and withhold a successful release-validation result. Preserve valid evidence, the option to record genuine failed outcomes, and the advisory when no baseline is configured. Verify the five behaviors in the existing .NET test projects.

</frozen-after-approval>

## Implementation Notes

The accessibility recorder now validates measured counts and ordinals before retaining a packet. Passed target geometry uses the same overlap predicate for equivalent controls and focus occluders, and the same spacing rules for the inventory and supplied neighbors. The skill corpus guard retains baseline diagnostics and stops comparing resources after a baseline error.

## Review Triage Log

- Fixed the conflicting bounds case for an equivalent control cited by ordinal. When the inventory contains that control, both measurements must agree.
- Fixed omitted spacing evidence from both the explicit neighbor list and equivalent controls recorded by reference. Added controls for both sources.
- Fixed overlap checks at large finite coordinates by comparing coordinate differences. The ordinary edge-contact control still passes.
- Removed the configured clock callback between validation and formatting. The recorder serializes the validated measurements before invoking it.
- Extended the baseline regression to a provider with parser errors and resources that would otherwise trigger drift validation.

## Verification

The original implementation failed all nine initial regression cases. After the fixes and independent review, both affected Release test projects built with warnings as errors using SDK `11.0.100` and the committed Builds catalog. `Hexalith.FrontComposer.Testing.Tests` passed 194 cases. `Hexalith.FrontComposer.Mcp.Tests` passed 557 cases, including the runtime package comparison against the restored `6.0.1` baseline and its benchmark assembly boundary check. Both suites reported zero errors, failures or skips.
