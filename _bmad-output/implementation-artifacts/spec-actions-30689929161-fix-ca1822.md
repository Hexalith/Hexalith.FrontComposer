---
title: 'Fix Actions 30689929161 CA1822 failure'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
route: 'one-shot'
---

# Fix Actions 30689929161 CA1822 failure

## Intent

**Problem:** GitHub Actions run `30689929161` fails its Release build because the plain-struct classification fixture exposes a constant instance property that triggers `CA1822` under warnings-as-errors. Once that build blocker is removed, the same revision's five newly added underscored test identifiers also require the fail-closed analyzer inventory to be re-sealed.

**Approach:** Keep the fixture member-bearing with an instance-backed auto-property, preserving its plain-struct classification purpose without suppressing analyzers. Refresh only the deterministic test identifier count and hash needed by the current revision.

## Suggested Review Order

**Build unblock**

- Use instance-backed state to satisfy CA1822 while preserving member-bearing struct coverage.
  [`ShellTypeOrganizationGovernanceTests.cs:804`](../../tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellTypeOrganizationGovernanceTests.cs#L804)

**Governance seal**

- Re-seal the intentional five-identifier test census exposed after the build advances.
  [`analyzer-policy-exception-ledger-v1.json:74`](../contracts/analyzer-policy-exception-ledger-v1.json#L74)

**Follow-up**

- Track record-struct synthesis hardening without broadening this CI repair.
  [`deferred-work.md:1899`](deferred-work.md#L1899)
