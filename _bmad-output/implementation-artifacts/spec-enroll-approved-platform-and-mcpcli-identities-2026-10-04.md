---
title: 'Enroll the approved Platform and McpCli dependency identities'
type: 'bugfix'
created: '2026-10-04'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Current committed EventStore and Memories graphs introduce Platform and McpCli identities that the active dependency policy rejects. The user explicitly approved both identities. Enroll them in the closed policy allowlist and document that approved depth-2 targets may have policy-owned acquisition paths without becoming root gitlinks. Keep graph depths, candidate URL rejection, static build command restrictions, base-policy activation and root-only submodule initialization unchanged. Record Platform as evidence-only because its selected file-based orchestration trees have no standalone .NET solution, and register McpCli's existing standalone Release/NuGet solution. Do not enroll any further identities or move dependency revisions.

</frozen-after-approval>

## Implementation Notes

- Intent gaps: none after the explicit approval of both identities. No external side effect is needed. Footprint: policy, three current authority documents, one boundary regression, and this record; collector APIs and executable behavior remain unchanged.
- Existing collector resolves policy-owned paths and traverses only root-selected owners. Adding these identities therefore retains 47 edges and seven Builds selectors; absent local stores for the depth-2 targets do not affect collection.
- Both identities use the shared catalog baseline profile. Platform has no Builds-selector owner role in this graph; its profile remains strict if that role is introduced later. Its evidence-only disposition does not suppress builds of affected EventStore/Memories owners.
- Trust changes cannot authorize their own landing: CI diff continues to use the previous committed base policy. Default current-commit governance consumes the newly committed policy only after the repair is recorded.

- Disposition source inspection used `git -C ../platform ls-tree -r --name-only <commit>` and `git -C ../mcpcli ls-tree -r --name-only <commit>` with output filtered to solution/build/orchestration files. EventStore selects Platform `7c2f0f89f29c79f5d7ab4b731155e2cb07fc690f` and McpCli `7e3226ba612a3e7fb3a8969c4197a8f1e4c0c2ed`; Memories selects Platform `2d1b76267bc6e75d2caab1019a17f1c45ceccf81` and McpCli `f91f44c3c1eed173483ccab192bf2abb7ff73368`. Both Platform trees contain `apphost.cs` and `DaprSelfHostedMtls.cs`, with no `.slnx`, root `Directory.Build.props`, or root `Directory.Packages.props`. Both McpCli trees contain `Hexalith.McpCli.slnx` and both build/catalog files. `git -C ../mcpcli show <commit>:Directory.Build.props` and `...:Directory.Packages.props` show .NET 10/C# 14, warnings as errors, central package management and guarded Builds imports. No selected source or checkout was modified.
- The independent review found no additional required policy patch. Its cross-cutting findings and dispositions are recorded in `spec-reconcile-successor-capture-target-2026-10-04.md`; synthetic boundary regression and the existing real-policy integration fact exercise complementary behavior.

- Verification: Debug Shell test-project build passed without warnings/errors; Python runtime evidence 208/208, dependency graph 102/102 and AppHost smoke 84/84 passed. Toolchain and runtime identity governance facts passed 2/2. Candidate policy validates 47 edges and seven selectors. Clean isolated successor preparation passes with approval explicitly open; 42 sealed files remain byte-identical. SDK follow-up review confirms no remaining issue. Committed-policy integration is run after recording the repair.

- Post-commit verification at `cfa4f374cd6b25817e33ae97a2c664e4b9c08d69`: the three original governance checks pass 3/3; default committed-policy validation passes 47 edges and seven selectors. No policy override or enforcement bypass was used.
