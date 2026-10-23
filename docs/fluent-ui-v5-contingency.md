---
title: Fluent UI v5 Contingency Plan
description: Resolve the selected Fluent identity, preserve its approval gates, and validate catalog-owned upgrades and rollback.
genre: how-to
audience: framework-contributor
ownerStory: 15.4
status: authored
reviewed: 2026-10-23
uid: frontcomposer.how-to.fluent-ui-v5-contingency
---

# Fluent UI v5 Contingency Plan

Use this plan when maintaining FrontComposer's Fluent dependency or assessing an upgrade. Package selection and Product acceptance have separate authorities.

## Selected package identity

The root `Directory.Packages.props` imports the central catalog at `references/Hexalith.Builds/Props/Directory.Packages.props`. The authoritative version is the catalog object at the Builds gitlink selected by **root HEAD**, regardless of the Builds working-tree or checkout state.

| Package ID | Selected version |
| --- | --- |
| `Microsoft.FluentUI.AspNetCore.Components` | `5.0.1` |
| `Microsoft.FluentUI.AspNetCore.Components.Icons` | `5.0.1` |

Both selections are **GA**. To inspect the authority, run these read-only commands from the FrontComposer root, substituting the exact gitlink SHA printed by the first command into the second:

```bash
git ls-tree HEAD -- references/Hexalith.Builds
git -C references/Hexalith.Builds show <selected-builds-sha>:Props/Directory.Packages.props
```

`pwsh ./eng/validate-docs.ps1` compares this table with those committed XML entries. Missing gitlinks, unavailable objects, ambiguous entries, unresolved versions and document drift block documentation validation. A local checkout version cannot replace missing authority.

## Decision and evidence requirements

The **2026-10-21 Product Owner + Architect decision is not approved**, as recorded in [Story 13.9's Product Decision Receipt](https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/_bmad-output/implementation-artifacts/spec-13-9-decide-the-exact-fluent-ui-v5-posture.md#product-decision-receipt). **OI-4, FLUENT-APP-1, D-13 and G-4 remain open.** Selecting GA packages, completing Story 13.9 or correcting this page confers no acceptance.

No single exact identity has green UX-A..UX-F evidence. The receipt's [hosted Quality run 39554582583](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/39554582583) is red: WebKit has 12 failures, Gate 2b fails and Gate 3a bUnit is skipped. Earlier evidence spans RC, GA 5.0.0 and GA 5.0.1 and remains bound to its own candidate, revision and executing identity. Equal package versions across Builds revisions do not transfer evidence between candidates.

The Quality/Test role must obtain **one green hosted Quality run**, cited by run URL, on a **main commit whose root gitlink selects the exact target identity**. It must cover Chromium, Firefox, WebKit, `accessibility-visual` and Gate 3a bUnit. Fix or explicitly disposition the 12 WebKit failures first; Gate 3a depends on the existing Gate 2b work. Then the **Product Owner + Architect** must make a dated re-decision citing that run, under DW-1985. A passing run alone cannot approve the posture.

Human assistive-technology, zoom and device observations remain separate evidence under **OI-16**, naming the same package identity; automated checks cannot supply them. G-4 also retains its prerequisite closures, refreshed digest-bound reviewer gate and D-9 re-approval. This page grants none of those approvals and does not rewrite historical evidence or accessibility statuses.

## Verified APIs and Fluent 2 tokens

Story 13.9 verified these names in the selected package. Validate the affected surface at every proposed identity; a supported API name does not promise FrontComposer's internal component composition.

| Surface | Verified names or contract | Upgrade check |
| --- | --- | --- |
| Providers and layout | `FluentProviders`, `FluentLayout`, `FluentStack`, `FluentGrid` | Provider resolution, layout regions, keyboard order and responsive behavior remain correct. |
| Presentation | `FluentAccordion`, `FluentBadge`, `FluentTooltip` | Accessible names, expansion state, visible status text and keyboard interaction remain correct. |
| Theme service | `IThemeService.SetThemeAsync(ThemeSettings)`, `ThemeMode` | Theme changes preserve the brand ramp and light/dark behavior. |
| Theme settings | `ThemeSettings(Color, HueTorsion, Vibrancy, Mode, IsExact)` | The shell seeds the ramp with `new ThemeSettings(AccentColor, 0, 0, mode, false)`. |
| FrontComposer surfaces | [Page toolbar](reference/components/page-toolbar.md), [DataGrid](reference/components/datagrid.md), [navigation](reference/components/navigation.md), [command lifecycle](reference/components/command-lifecycle.md) | Validate public FrontComposer contracts, generated bindings, focus, sorting and lifecycle announcements against the target package. |

The active accent foreground role is **`--colorBrandForeground1`**. The seed configures the brand ramp; it does not authorize using accent as a chrome surface fill or inventing a palette. Supported Fluent 2 token names verified by the receipt are:

- `--colorBrandForeground1`, `--colorBrandBackground`.
- `--colorNeutralBackground1`, `--colorNeutralBackground2`, `--colorNeutralBackground3`.
- `--colorNeutralStroke2`.
- `--colorNeutralForeground1`, `--colorNeutralForeground2`, `--colorNeutralForeground3`.
- `--colorStatusSuccessForeground1`, `--colorStatusWarningForeground1`, `--colorStatusDangerForeground1`.

**Unsupported:** `--colorStatusInfoForeground1` does not resolve in 5.0.1. Consequently, the existing `--fc-color-info` alias is unresolvable. It has no runtime consumers but is pinned by `SlotMappingRegressionTests`; **DW-1986** owns its disposition. Do not copy that alias as supported guidance or treat this correction as an alias fix.

Legacy v4/FAST tokens, including `--type-ramp-*`, `--neutral-*`, `--accent-*`, `--neutral-fill-*` and `--palette-*`, remain outside the supported Fluent 2 contract. Use the verified Fluent 2 roles and existing FrontComposer customization seams.

## Catalog-owned upgrade procedure

1. Record both target package IDs and exact literal versions, the selecting Builds revision and the FrontComposer candidate. Coordinate version changes in the **Hexalith.Builds repository**; its catalog owns the pins. Do not add project-level versions, local catalog overrides or duplicate root entries. Updating FrontComposer's selected root gitlink is a separate reviewed dependency change.
2. Restore and build the affected projects using the repository's dependency mode: local development uses Debug project references, while CI uses Release packages. Build test projects individually and run their current relevant suites, including Fluent conformance, token mapping, package boundaries, generated forms, navigation and DataGrid behavior. Use current test discovery and gate results rather than a historical fixed test count.
3. Revalidate conformance and accessibility/visual behavior at the exact proposed identity, including the shell accent-as-thread rule. Preserve every existing failure, skip, candidate binding and human evidence requirement. Never project earlier evidence onto the proposed identity.
4. Run `pwsh ./eng/validate-docs.ps1` after reconciling the identity table with the selected catalog. Passing documentation validation establishes identity and publication parity; acceptance still follows the decision receipt.
5. Obtain the required hosted Quality evidence and dated owner decision before claiming acceptance. Review the whole dependency change and document the affected APIs and migration/deprecation handling.

The GA re-decision trigger fired on **2026-09-30** and was consumed by the 2026-10-21 negative decision. While the target remains unapproved, obtain the Product Owner + Architect re-decision described above. **After acceptance**, a **patch** bump requires a green Quality run on the bump commit plus a dated one-line **Architect addendum** to the receipt naming the exact version and run URL. A **minor or major** bump requires a **new Product Owner + Architect decision**. Every identity change requires conformance and accessibility/visual revalidation. A **breaking Fluent change requires a FrontComposer major version**, with conformance and accessibility/visual evidence under **D-13**.

## Rollback and contingency

If validation fails, stop adoption and record the failing surface, exact package identity, selecting revision and evidence. Coordinate a reviewed rollback through **Hexalith.Builds**, then select the intended root Builds gitlink through the normal dependency workflow. Review the full catalog impact because a Builds revision can change other dependencies. Restore, build and re-run the affected checks at the rollback identity, and reconcile this table and the decision receipt as required. Returning to an earlier selection does not make its evidence current or confer acceptance.

Do not use local `dotnet add package` pins, project-level overrides or partial shims as a substitute for a catalog-owned rollback. Keep the failure and prior decision records intact for the next owner decision.

The Fluent canary workflow is **not implemented**. There is no scheduled Fluent prerelease qualification or advisory canary result to rely on. Any future canary implementation is separate work and cannot replace the hosted Quality, human evidence or owner decision gates.

Maintainers can manually watch [Fluent UI Blazor releases](https://github.com/microsoft/fluentui-blazor/releases) through a team-owned watcher account. Release notifications initiate assessment; they do not change the selected catalog or approval posture.
