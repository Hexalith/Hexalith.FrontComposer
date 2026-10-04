---
title: 'Reconcile the current EventStore successor capture target'
type: 'bugfix'
created: '2026-10-04'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

The mutable runtime successor target still names older EventStore and Builds revisions, so the governance gate rejects the committed dependency selection. Reconcile the Python target, both Quality workflow lanes, governance assertions and operator instructions to committed EventStore `5e32d07a6ac7a1bf70cc0ca554ea9928145b65f6`, Builds `688eec9a4333245cc0ff7772115c769094471863` and catalog version `3.110.0`. Align the stale Aspire governance expectation with the existing `13.6.0` pins. Preserve sealed identities, historical evidence, open migration approval and the absence of identity v4. This repair prepares a future hosted capture; it does not manufacture or approve that capture.

</frozen-after-approval>

## Implementation Notes

- Intent gaps: none for this local repair. No external action is required to reconcile the target. Footprint: four existing files and this record; no API or dependency revision changes.
- Latest investigated root commit: `a93e4e5bc3cf1d1fc414a074578f3924882a7eea`; EventStore/Builds selections were read from its committed tree, and version `3.110.0` from the committed Builds catalog.
- Historical live evidence remains the sealed v2 tuple, already allowed independently of the successor. No new historical acceptance branch is needed. The new target's ProjectionBacked query contract matches committed EventStore routing and Tenants metadata.
- Platform policy enrollment is a separate prerequisite: approved Platform is absent from the root dependency declarations; current nested graphs also introduce McpCli. Investigation must preserve the closed root identity boundary.

- Review exposed the stale AppHost SDK package-ledger pin. The producer now binds `13.6.0`, while validation selects `13.5.4` for the exact sealed v3 tuple and `13.6.0` for the exact successor tuple using authenticated provenance. Unknown tuples and crossed SDK versions fail. Historical evidence bytes remain unchanged. Added regressions in `tests/eng/test_eventstore_runtime_evidence.py` and an exact producer SDK line check to the existing toolchain governance fact.
- The ordinary workspace preparation check rejected generated Counter AppHost files, Dapr name-resolution files and Parties output inputs. These ambient files were preserved. Clean preparation is checked in temporary local clones of only root-declared repositories at exact committed selections, without nested initialization or live capture.

## Review Triage Log

- **high / patch:** stale SDK ledger requested `13.5.4` despite current SDK `13.6.0`; split current producer and authenticated historical/successor validation contracts, retaining historical bytes.
- **false:** the synthetic depth boundary regression need not duplicate policy rows. The existing `CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` fact validates the real committed policy and actual dependency graph; removing an approved identity fails that integration gate. Policy loading also requires exact profile and registry coverage.
- **medium / patch:** operator instructions omitted first-push base-policy activation failure; documented it explicitly while preserving immutable-base enforcement.
- **low / patch:** disposition evidence lacked exact source coordinates; recorded all four selected commits and object-only inspection commands in the enrollment record.

- Verification: Debug Shell test-project build passed without warnings/errors; Python runtime evidence 208/208, dependency graph 102/102 and AppHost smoke 84/84 passed. Toolchain and runtime identity governance facts passed 2/2. Candidate policy validates 47 edges and seven selectors. Clean isolated successor preparation passes with approval explicitly open; 42 sealed files remain byte-identical. SDK follow-up review confirms no remaining issue. Committed-policy integration is run after recording the repair.

- Post-commit verification at `cfa4f374cd6b25817e33ae97a2c664e4b9c08d69`: the three original governance checks pass 3/3; default committed-policy validation passes 47 edges and seven selectors. No policy override or enforcement bypass was used.
