---
workflow: bmad-correct-course
date: 2026-06-26
mode: Batch
status: Implemented
change_trigger: Move root-declared Hexalith submodules under references/
scope_classification: Moderate
approval: User directive in request, 2026-06-26
---

# Sprint Change Proposal: Move Submodules To `references/`

## 1. Issue Summary

FrontComposer root submodules were checked out as top-level `Hexalith.*` directories while sibling
Hexalith repositories already standardize root-declared submodules under `references/`. The top-level
layout made root submodules visually collide with nested submodule names and kept the agent/project
instructions out of sync with the intended ecosystem policy.

Evidence:

- `.gitmodules` declared root submodules at `Hexalith.EventStore`, `Hexalith.Tenants`,
  `Hexalith.Commons`, `Hexalith.Builds`, `Hexalith.PolymorphicSerializations`,
  `Hexalith.AI.Tools`, and `Hexalith.Memories`.
- `Hexalith.FrontComposer.slnx`, `deps.local.props`, AppHost project metadata, generated BMAD docs,
  and root agent instructions referenced the old root-folder layout.
- The root repository policy already forbids recursive nested submodule initialization.

## 2. Impact Analysis

Epic impact: no epic is added, removed, or resequenced. This is a cross-cutting repository layout NFR
change. `epics.md` now records the `references/Hexalith.*` layout as NFR14.

Story impact: current and future stories that cite root submodule paths must use
`references/Hexalith.*`. Historical sprint records are left intact unless they are current generated
project context.

Artifact impact:

- `.gitmodules`: submodule paths move under `references/`.
- `Hexalith.FrontComposer.slnx`: external project references move to `references/...`.
- `deps.local.props`: local `EventStorePath` and `TenantsPath` move to `references/...`.
- AppHost `IProjectMetadata`: cross-repository project path resolver prepends `references/`.
- Root LLM instructions: `AGENTS.md` and `CLAUDE.md` point to
  `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- Docs: BMAD project context/docs, test guide, diagnostic sample, and Fluent contingency snippet use
  root-declared `references/` submodules and no recursive checkout.
- Tests: diagnostic-registry governance expects `.gitmodules` paths under `references/`.

Technical impact: `git submodule update --init` from the superproject initializes only root-declared
submodules into `references/`. Existing nested submodule policy remains unchanged: never use recursive
submodule update and de-initialize nested submodules if they appear.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale: the change is structural and local to repository metadata, build path configuration, AppHost
path resolution, tests, and docs. It does not require rollback, PRD scope reduction, or epic replan.

Effort: Medium. The move touches gitlinks plus multiple path consumers.

Risk: Medium. Build and test risk is concentrated in stale path references and submodule worktree
metadata, not runtime behavior.

## 4. Checklist Outcome

- [x] 1.1 Trigger identified: repository layout correction for root-declared submodules.
- [x] 1.2 Core problem defined: old root-folder submodule layout conflicts with desired
  `references/` ecosystem convention.
- [x] 1.3 Evidence gathered: `.gitmodules`, `.slnx`, `deps.local.props`, AppHost metadata, docs, and
  tests referenced old paths.
- [x] 2.1-2.5 Epic impact assessed: no epic replan; add cross-cutting NFR only.
- [x] 3.1 PRD impact assessed: no authored PRD exists; `epics.md` is the current requirements inventory.
- [x] 3.2 Architecture impact assessed: external dependency section and path resolver updated.
- [N/A] 3.3 UI/UX impact: no user-facing UI surface changes.
- [x] 3.4 Secondary artifacts assessed: workflows, diagnostic docs, test guide, and generated BMAD docs
  reviewed.
- [x] 4.1 Direct adjustment viable: yes.
- [N/A] 4.2 Rollback path: not useful.
- [N/A] 4.3 MVP review: no MVP scope impact.
- [x] 4.4 Path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal and handoff captured here.
- [x] 6.1-6.5 Final review and handoff: implementation routed to Developer agent in this turn.

## 5. Detailed Change Proposals

### `.gitmodules`

OLD:

```ini
path = Hexalith.EventStore
path = Hexalith.Tenants
path = Hexalith.Commons
path = Hexalith.Builds
path = Hexalith.PolymorphicSerializations
path = Hexalith.AI.Tools
path = Hexalith.Memories
```

NEW:

```ini
path = references/Hexalith.EventStore
path = references/Hexalith.Tenants
path = references/Hexalith.Commons
path = references/Hexalith.Builds
path = references/Hexalith.PolymorphicSerializations
path = references/Hexalith.AI.Tools
path = references/Hexalith.Memories
```

Rationale: keeps all root-declared submodules in one explicit external-dependency folder.

### Solution And Project Paths

OLD:

```xml
<Project Path="Hexalith.EventStore/src/..." />
<EventStorePath>$(MSBuildThisFileDirectory)Hexalith.EventStore</EventStorePath>
```

NEW:

```xml
<Project Path="references/Hexalith.EventStore/src/..." />
<EventStorePath>$(MSBuildThisFileDirectory)references/Hexalith.EventStore</EventStorePath>
```

Rationale: restores build and IDE solution resolution after the gitlink move.

### AppHost Metadata

OLD:

```csharp
Path.Combine(GetRepositoryRoot(), Path.Combine(path));
```

NEW:

```csharp
Path.Combine(GetRepositoryRoot(), "references", Path.Combine(path));
```

Rationale: AppHost hosted-service project locators continue to find EventStore/Tenants projects without
making those projects normal AppHost project references.

### LLM Instructions

OLD:

```markdown
./Hexalith.AI.Tools/hexalith-llm-instructions.md
```

NEW:

```markdown
./references/Hexalith.AI.Tools/hexalith-llm-instructions.md
```

Rationale: agents load the required Hexalith instructions from the new submodule location before work.

### Documentation And Tests

OLD:

```text
root-level submodules: Hexalith.Commons, Hexalith.EventStore, Hexalith.Tenants
submodules: recursive
declaredPaths.ShouldContain("Hexalith.EventStore")
```

NEW:

```text
root-declared submodules under references/Hexalith.*
submodules: true
declaredPaths.ShouldContain("references/Hexalith.EventStore")
```

Rationale: documentation and governance tests must encode the same submodule policy as `.gitmodules`.

## 6. Implementation Handoff

Scope classification: Moderate.

Route: Developer agent direct implementation.

Responsibilities:

- Move root gitlinks to `references/`.
- Update build/solution/AppHost path consumers.
- Update root LLM instructions and current project docs.
- Validate no nested submodule remains initialized.
- Run focused restore/build/tests where feasible.

Success criteria:

- `git submodule status` shows all root-declared submodules under `references/`.
- The nested-submodule verification command prints nothing.
- `dotnet restore Hexalith.FrontComposer.slnx` can resolve the moved project paths.
- Focused governance tests for CI submodule policy and diagnostic submodule boundaries pass.
