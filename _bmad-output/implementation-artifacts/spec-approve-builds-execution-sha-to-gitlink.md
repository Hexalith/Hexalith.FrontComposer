---
title: 'Approve Builds execution SHA to current gitlink'
type: 'bugfix'
created: '2026-08-05'
status: 'done'
route: 'one-shot'
---

# Approve Builds execution SHA to current gitlink

## Intent

**Problem:** Release run 31006743353 failed in `verify-source` because the approved Builds execution SHA (`a5316653…`) no longer matched the `references/Hexalith.Builds` gitlink after recent submodule bumps (`bd94f7fe…`, BUILD-REL-1).

**Approach:** Re-approve that exact gitlink as the release execution identity across `release.yml`, `release-evidence.yml`, and the operator/docs that name the pin; keep all literal coordinates identical.

## Suggested Review Order

**Release identity lockstep**

- Env and reusable pin must equal the Builds gitlink.
  [`release.yml:17`](../../../.github/workflows/release.yml#L17)

- Reusable `uses:@` and `builds-execution-sha` stay identical literals.
  [`release.yml:277`](../../../.github/workflows/release.yml#L277)

- Evidence checkout uses the same approved Builds commit.
  [`release-evidence.yml:223`](../../../.github/workflows/release-evidence.yml#L223)

**Operator docs**

- Deployment guide records the new identity and publish-freeze variable.
  [`deployment-guide.md:3`](../project-docs/deployment-guide.md#L3)
