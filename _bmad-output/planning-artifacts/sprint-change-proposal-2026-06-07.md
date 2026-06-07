# Sprint Change Proposal — Aspire.Hosting.Keycloak version pin

- **Project:** Hexalith.FrontComposer
- **Date:** 2026-06-07
- **Author:** Administrator (via BMAD Correct Course)
- **Change scope classification:** Minor (dependency-governance decision)
- **Recommended outcome:** **HOLD** — do not bump; decline `13.4.2-preview`

---

## Section 1 — Issue Summary

**Trigger:** A "update to latest Aspire" pass flagged `Aspire.Hosting.Keycloak`, currently pinned at
`13.4.0-preview.1.26281.18` in `Directory.Packages.props`, as a candidate for a version bump. A newer
`13.4.2-preview` exists on NuGet.

**Core problem (categorized: *technical constraint discovered during a routine dependency sweep*):**
"Update to latest" conflicts with a deliberate, documented cross-repo alignment invariant. The pin
carries the comment:

> `Preview version aligned with the Hexalith.Tenants / Hexalith.EventStore AppHosts.`

**Evidence collected:**

- `Directory.Packages.props:50-53` — `Aspire.Hosting.AppHost` = `13.4.2` (stable), but
  `Aspire.Hosting.Keycloak` = `13.4.0-preview.1.26281.18` (preview), with the alignment comment.
- `Hexalith.EventStore/Directory.Packages.props:15` and `Hexalith.Tenants/Directory.Packages.props:15`
  — **both sibling submodules pin `Aspire.Hosting.Keycloak` to exactly `13.4.0-preview.1.26281.18`.**
- Sibling props reveal the pattern is **intentional and selective**, not stale: in both submodules,
  `Aspire.Hosting` / `Docker` / `Redis` / `Testing` / `Azure.AppContainers` ride the stable `13.4.2`
  line, while **only** `Aspire.Hosting.Keycloak` *and* `Aspire.Hosting.Kubernetes` are held at the
  `13.4.0-preview.1.26281.18` preview — because those two integrations have no stable `13.4.2` release.
  The ecosystem deliberately pins the exact known-good preview build for reproducibility.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` consumes the pin via a
  versionless `<PackageReference Include="Aspire.Hosting.Keycloak" />` (central versioning), so the pin
  in `Directory.Packages.props` is the single source of truth for this AppHost.

---

## Section 2 — Impact Analysis

| Area | Impact | Notes |
|------|--------|-------|
| **Epics** | None | `epics.md` has **zero** mentions of Aspire/Keycloak/`13.4`. No epic owns this dependency; no epic is blocked or unblocked by it. |
| **Stories** | None | Not tied to any current or future story. This is an infra-governance decision, not feature work. |
| **PRD** | None | MVP scope unaffected; no requirement depends on a Keycloak hosting version. |
| **Architecture** | None | No component, pattern, contract, schema, or integration point changes. Keycloak is the local OIDC/JWT identity provider for the AppHost only. |
| **UI/UX** | None | No surface impact. |
| **CI / build** | Risk if bumped | A solo bump diverges FrontComposer from the exact preview build the rest of the ecosystem validates against; the AppHost csproj already suppresses Aspire transitive audit warnings (`NU1902/1903/1904`), so a preview drift would not be caught there. |
| **Submodules** | Blocking constraint | Project rule: *"never modify submodule files without explicit approval — changes propagate across the Hexalith ecosystem."* Aligning the siblings *up* to `13.4.2-preview` is therefore out of bounds for this sprint. |

**Secondary observation (doc drift, low priority, out of this change's scope):**
`_bmad-output/project-context.md:41` states `Aspire.Hosting.AppHost 13.4.0`, but the actual pin
(`Directory.Packages.props:50`) and the AppHost SDK are at `13.4.2`. Worth a one-line refresh in a
future housekeeping pass.

---

## Section 3 — Path Forward Evaluation

| Option | Verdict | Effort / Risk | Rationale |
|--------|---------|---------------|-----------|
| **A. Hold at `13.4.0-preview.1.26281.18`** *(recommended)* | ✅ Viable | Low / Low | Preserves the documented cross-AppHost alignment and the exact known-good preview build. No code change required. |
| B. Bump FrontComposer only → `13.4.2-preview` | ❌ Not viable | Low / **High** | Breaks the alignment invariant; diverges from the build the siblings validate against; gains nothing functional. |
| C. Coordinated ecosystem bump (Tenants + EventStore + FrontComposer in lockstep) | ⚠️ Deferred | High / Medium | The *only* legitimate way to "go newer," but requires explicit submodule-modification approval and is a separate, owned story across three repos. Not justified by a routine sweep. |

**Selected approach: Option A — Direct Adjustment (Hold).** The "latest" pull is intentionally
declined. The preview pin is not technical debt; it is a deliberate ecosystem-wide reproducibility
anchor for the two Aspire integrations (Keycloak, Kubernetes) that lack a stable `13.4.2` line.

---

## Section 4 — Detailed Change Proposals

**No version change.** `Aspire.Hosting.Keycloak` stays at `13.4.0-preview.1.26281.18`.

**Optional durability edit (recommended, Minor):** strengthen the comment in
`Directory.Packages.props` so the next dependency sweep does not re-litigate this — recording *why*
it is a preview and that it must not be bumped independently.

```text
Directory.Packages.props (ItemGroup Label="Aspire")

OLD:
    <!-- Keycloak hosting for the FrontComposer AppHost (OIDC/JWT identity provider).
         Preview version aligned with the Hexalith.Tenants / Hexalith.EventStore AppHosts. -->
    <PackageVersion Include="Aspire.Hosting.Keycloak" Version="13.4.0-preview.1.26281.18" />

NEW:
    <!-- Keycloak hosting for the FrontComposer AppHost (OIDC/JWT identity provider).
         Pinned to the exact preview build shared by the Hexalith.Tenants / Hexalith.EventStore
         AppHosts. There is no stable 13.4.x Keycloak-hosting line, so the ecosystem holds this
         known-good preview for reproducibility. Do NOT bump independently (e.g. to 13.4.2-preview):
         it must move in lockstep with the sibling AppHosts in an owned, submodule-approved story. -->
    <PackageVersion Include="Aspire.Hosting.Keycloak" Version="13.4.0-preview.1.26281.18" />
```

*Rationale:* turns an implicit alignment convention into an explicit, self-documenting guardrail.
Comment-only; no behavior, build, or dependency-graph change.

---

## Section 5 — Implementation Handoff

- **Scope:** Minor → Developer agent, direct implementation (only if the optional comment edit is approved).
- **If Hold-only is chosen:** no implementation required; this proposal *is* the deliverable (the
  decision record). Close out.
- **If a future newer Aspire is genuinely needed:** route to PM/Architect to scope Option C — a
  coordinated, submodule-approved, lockstep bump across Tenants + EventStore + FrontComposer.
- **Success criteria:** `Directory.Packages.props` Keycloak pin remains `13.4.0-preview.1.26281.18`
  and matches both sibling submodules; `dotnet build -c Release` stays clean if the comment is edited.
- **Sprint-status:** no epic/story add/remove/renumber → no `sprint-status.yaml` update needed.

---

## Decision log

- **2026-06-07** — Reviewed under BMAD Correct Course. Recommendation: **HOLD** at
  `13.4.0-preview.1.26281.18`; decline `13.4.2-preview`.
- **2026-06-07** — **APPROVED by user (Hold + harden comment).** Optional comment-hardening edit
  applied to `Directory.Packages.props` (comment-only; pin unchanged). No epic/story/sprint-status
  changes. Change closed.
