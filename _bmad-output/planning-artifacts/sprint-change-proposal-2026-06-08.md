# Sprint Change Proposal — Consolidate to a single Aspire host (fold Counter into the platform host)

- **Project:** Hexalith.FrontComposer
- **Date:** 2026-06-08
- **Author:** Administrator (via BMAD Correct Course)
- **Change scope classification:** Minor (contained dev refactor — no epic/story/PRD/backlog change)
- **Selected approach:** Option 1 — Direct Adjustment (co-host only)
- **Status:** APPROVED & IMPLEMENTED (2026-06-08) on branch `refactor/single-aspire-host` — pending review/PR

---

## Section 1 — Issue Summary

**Trigger (direct architectural decision, not a story):** the repository should have **only one
Aspire host**, and the Counter example should live inside that single host alongside all other
services (eventstore, tenants, tenants-ui, counter, etc.).

**Core problem (categorized: *misunderstanding/drift of the intended topology discovered during
review*):** the repo currently ships **two** Aspire hosts:

| Host | Orchestrates | How it references projects |
|------|--------------|----------------------------|
| `src/Hexalith.FrontComposer.AppHost` (platform host) | keycloak, eventstore, eventstore-admin, eventstore-admin-ui, tenants, **tenants-ui**, full DAPR topology (state store + pub/sub, sidecars, resiliency) | cross-repo submodule projects via `IProjectMetadata` (`SuppressBuild=true`) + the `Hexalith.EventStore.Aspire` library |
| `samples/Counter/Counter.AppHost` (isolated) | only `counter-web` | local `ProjectReference` → `Projects.Counter_Web` |

The Counter sample runs in its own isolated host instead of the platform host, so there is no single
orchestrator for the whole stack.

**Evidence collected:**

- `src/Hexalith.FrontComposer.AppHost/Program.cs` — orchestrates the full platform (keycloak +
  eventstore + ×2 admin + tenants + tenants-ui + DAPR); **does not** include the Counter sample.
- `samples/Counter/Counter.AppHost/Program.cs:3` — `builder.AddProject<Projects.Counter_Web>("counter-web")`;
  the entire host is just this one resource.
- `Hexalith.FrontComposer.slnx:13` — both AppHosts are registered in the solution.
- **CI/e2e do not depend on `Counter.AppHost`** — they target `Counter.Web` directly:
  `tests/e2e/playwright.config.ts:50` (`dotnet run --project …/Counter.Web.csproj`) and
  `.github/workflows/ci.yml:454` (`dotnet build …/Counter.Web.csproj`). Removing the isolated host is
  therefore safe for the specimen/a11y/visual gate.

---

## Section 2 — Impact Analysis

| Area | Impact | Notes |
|------|--------|-------|
| **Epics** | None | `epics.md` has **zero** Aspire/AppHost/Counter mentions. No epic is blocked, added, removed, or resequenced. |
| **Stories** | None | No story owns either AppHost. In `sprint-status.yaml`, all Counter references are to `Counter.Web`/`Counter.Domain` (specimen host + File Lists), never `Counter.AppHost`. |
| **PRD** | None | No standalone PRD (project uses an epics + contracts model); MVP scope is unaffected — this is infra topology. |
| **Architecture** | Minor (doc) | `architecture.md` does not document the host topology. `project-context.md:41` is **stale** — it claims "only the `samples/Counter` AppHost" — and must be corrected to describe the single platform host. |
| **UI/UX** | None | No surface change. |
| **CI / build** | Low | The solution drops one project (`Counter.AppHost`); the platform AppHost gains a `ProjectReference` to `Counter.Web`. CI's specimen gate (builds `Counter.Web` directly) is unaffected. |
| **Tests / e2e** | Low | `playwright.config.ts` and the Level-3 spec launch `Counter.Web` directly — unaffected. Only doc/comment references to the removed host (`tests/README.md`, `tests/e2e/.env.example`) need updating. |
| **Docs** | Low | `_bmad-output/project-docs/development-guide.md:129` describes the now-removed `Counter.AppHost`. The published, CI-gated `docs/hot-reload-guide.md:83` names it once in a *historical run record* (prose only; not a validated snippet/link). |
| **Submodules** | None | No submodule files are touched; `Counter.Web` is local. |

---

## Section 3 — Recommended Approach

**Selected: Option 1 — Direct Adjustment, co-host only.**

- **Direct Adjustment** *(chosen)* — fold `counter-web` into `Hexalith.FrontComposer.AppHost`, delete
  `Counter.AppHost`, scrub references. Effort **Low**, Risk **Low**.
- **Rollback** — N/A (nothing to revert).
- **MVP review** — N/A (MVP unaffected).

**Wiring decision (user-selected): co-host only.** `counter-web` is added as a resource in the single
host but stays a self-contained demo (its current fake auth + seeded specimen data) and is
intentionally **not** wired to the eventstore/tenants/keycloak backend. Rationale: the a11y/visual
specimen gate and e2e suite run `Counter.Web` with fakes/seed data
(`Hexalith__FrontComposer__Specimens__Enabled`), so wiring it to a live backend would risk
destabilizing that gate for no functional benefit to the change request.

**Rationale:** lowest-effort path that fully satisfies "one Aspire host containing all services incl.
counter," with no epic/story/PRD/backlog impact and no submodule changes. Risk is confined to a build
topology edit plus reference scrubs; CI's Counter coverage is untouched because it targets `Counter.Web`
directly.

---

## Section 4 — Detailed Change Proposals

### Cluster A — Core topology (approved)

**A1 — `src/Hexalith.FrontComposer.AppHost/Program.cs`** — add `counter-web` as a co-hosted resource
(insert after the `tenants-ui` block, ~line 67):

```csharp
// Counter sample — the FrontComposer demo shell. Co-hosted in the single platform AppHost so the
// whole stack (EventStore, Tenants, Tenants UI, Counter) runs from ONE orchestrator. It stays a
// self-contained demo (fake auth + seeded specimen data) and is intentionally NOT wired to the
// EventStore/Tenants/Keycloak backend, to keep the a11y/visual specimen gate deterministic.
IResourceBuilder<ProjectResource> counterWeb = builder.AddProject<Projects.Counter_Web>("counter-web")
    .WithExternalHttpEndpoints();
```

**A2 — `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`** — add the
ProjectReference that generates `Projects.Counter_Web`:

```xml
  <ItemGroup>
    <!-- Counter sample web shell, co-hosted as a resource in the single platform AppHost. -->
    <ProjectReference Include="..\..\samples\Counter\Counter.Web\Counter.Web.csproj" />
  </ItemGroup>
```

*Note:* `Counter.Web` is local, so the standard Aspire `ProjectReference` → `Projects.*` codegen is
correct here (unlike the cross-repo submodule projects, which use `IProjectMetadata` + `SuppressBuild`).

**A3 — Delete `samples/Counter/Counter.AppHost/`** entirely (`Program.cs`,
`Counter.AppHost.csproj`, `Properties/launchSettings.json`, and build artifacts).

**A4 — `Hexalith.FrontComposer.slnx`** — remove line 13:

```diff
-    <Project Path="samples/Counter/Counter.AppHost/Counter.AppHost.csproj" />
```

The `/samples/Counter/` folder node and its other projects (`Counter.Specimens`,
`Counter.Specimens.Domain`, `Counter.Domain`, `Counter.Web`) remain.

### Cluster B — Documentation & reference scrubs (approved)

**B1 — `tests/README.md` (~line 104):**

```diff
-# Start the Counter sample (in another terminal)
-dotnet run --project samples/Counter/Counter.AppHost
+# Start the app under test (in another terminal):
+#   • Full platform (counter-web hosted alongside eventstore/tenants/tenants-ui):
+dotnet run --project src/Hexalith.FrontComposer.AppHost
+#   • Or just the Counter specimen shell (what the e2e webServer auto-launches):
+dotnet run --project samples/Counter/Counter.Web
```

**B2 — `tests/e2e/.env.example` (lines 7–9):**

```diff
 # Base URL of the Blazor web app under test.
-# For the Counter sample, start the AppHost first (dotnet run --project samples/Counter/Counter.AppHost)
-# and use the URL it logs for the Counter.Web service.
+# For the Counter sample, either run the single platform AppHost
+# (dotnet run --project src/Hexalith.FrontComposer.AppHost) and use the counter-web URL from the
+# Aspire dashboard, or run the specimen shell directly
+# (dotnet run --project samples/Counter/Counter.Web) — what the e2e webServer auto-launches.
```

**B3 — `_bmad-output/project-docs/development-guide.md:129`:**

```diff
-| `samples/Counter/` | Full **Aspire-orchestrated** example: `Counter.AppHost` (Aspire), `Counter.Domain`, `Counter.Web` (Blazor shell host), `Counter.Specimens(.Domain)` (a11y/visual specimen host). Run via the Aspire CLI (`aspire run`) on the AppHost. |
+| `samples/Counter/` | FrontComposer demo: `Counter.Domain`, `Counter.Web` (Blazor shell host), `Counter.Specimens(.Domain)` (a11y/visual specimen host). The `counter-web` resource is orchestrated by the **single platform host** `src/Hexalith.FrontComposer.AppHost` (`aspire run` / `dotnet run` on that host); `Counter.Web` also runs standalone for the specimen/e2e gate. |
```

**B4 — `_bmad-output/project-context.md:41-42`** (also folds in the `13.4.0 → 13.4.2` doc-drift fix
flagged in the 2026-06-07 proposal):

```diff
-- **Orchestration:** `Aspire.Hosting.AppHost` **13.4.0** — only the `samples/Counter` AppHost
-  (FrontComposer itself ships no deployed service)
+- **Orchestration:** `Aspire.Hosting.AppHost` **13.4.2** — a SINGLE AppHost,
+  `src/Hexalith.FrontComposer.AppHost`, orchestrates the whole local stack (keycloak, eventstore +
+  admin/admin-ui, tenants, tenants-ui, and the `counter-web` sample) via DAPR. FrontComposer itself
+  ships no deployed service; the sample shell (`samples/Counter/Counter.Web`) also runs standalone
+  for the a11y/visual specimen gate.
```

**B5 — `docs/hot-reload-guide.md:83` (PUBLISHED, CI-gated) — DECISION: leave as-is.**
Row 1.2 is a *historical run record* ("…Hot reload succeeded for `Counter.AppHost` and `Counter.Web`").
It is a dated past observation rendered as prose (not a CI-validated snippet or link), so it does not
break the docs gate and rewriting it would falsify the record. **No edit.** (Alternative on request: a
one-line footnote noting the later consolidation.)

---

## Section 5 — Implementation Handoff

- **Change scope:** **Minor** → Developer agent, direct implementation. No backlog reorganization, no
  replan; no epic/story/PRD changes.
- **Recipient:** Developer agent (`/bmad-dev-story` or `/bmad-quick-dev`).
- **Implementation order:** A2 (add ProjectReference) → A1 (add resource in Program.cs) → build & run
  the AppHost to confirm `counter-web` appears in the dashboard → A3 (delete `Counter.AppHost`) → A4
  (remove from `.slnx`) → B1–B4 (doc/reference scrubs). B5: no action.
- **Verification / success criteria:**
  - `dotnet build -c Release` on `Hexalith.FrontComposer.slnx` is clean (TreatWarningsAsErrors).
  - `dotnet run --project src/Hexalith.FrontComposer.AppHost` shows **one** host with a `counter-web`
    resource alongside eventstore/tenants/tenants-ui (Aspire dashboard).
  - `samples/Counter/Counter.AppHost` no longer exists; `grep -r "Counter.AppHost"` returns only the
    intentionally-retained historical line in `docs/hot-reload-guide.md`.
  - Default test lane + Governance/Contract tests green
    (`dotnet test … --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`,
    `DiffEngine_Disabled=true`).
  - e2e specimen gate unaffected (still launches `Counter.Web` directly).
- **Commit guidance:** Conventional Commit `refactor:` (topology change, **not** `feat`), on a
  `refactor/<desc>` branch → PR to `main`; run `/bmad-code-review` before Done.
- **Sprint-status:** no epic/story add/remove/renumber → **no `sprint-status.yaml` update needed.**

---

## Decision log

- **2026-06-08** — Reviewed under BMAD Correct Course. Two Aspire hosts confirmed; consolidation to the
  single `Hexalith.FrontComposer.AppHost` with **co-host-only** Counter wiring selected by user.
  Cluster A (topology) and Cluster B (docs/refs) approved incrementally; B5 left as-is. Scope: Minor →
  Developer agent.
- **2026-06-08** — **APPROVED by user (implement now).** All edits applied on branch
  `refactor/single-aspire-host`: A1/A2 (counter-web added to platform host + ProjectReference),
  A3 (`Counter.AppHost` deleted), A4 (removed from `.slnx`), B1–B4 (doc/reference scrubs); B5 left
  as-is. `dotnet build -c Release` of `Hexalith.FrontComposer.AppHost` = **0 warnings / 0 errors**
  (`Counter.Web` builds in-host, `Projects.Counter_Web` resolves). Only remaining `Counter.AppHost`
  reference is the intentional historical line in `docs/hot-reload-guide.md:83`. Not yet committed —
  awaiting user review + `/bmad-code-review` before PR to `main`.
