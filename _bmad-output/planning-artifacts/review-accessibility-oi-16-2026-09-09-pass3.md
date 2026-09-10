# OI-16 Accessibility Review — Pass 3

Date: 2026-09-09
Lens: independent accessibility-contract review using the BMad UX reviewer rubric
Scope: the frozen three-file D-8 UX authority chain, checked against PRD FR-8, FR-10–FR-16,
FR-22, FR-23, NFR-3, SM-6, Addendum §4, repository accessibility-verification guidance, and
representative source/test evidence used to distinguish the delivered baseline from open work.

## Reviewed Revisions

| Authority | SHA-256 |
| --- | --- |
| `ux-design.md` | `8597dc1dff170ddb43bc521bbe5a04eafe3ccb9b66492ebae53c578999c44e08` |
| `ux-design-detailed-2026-07-05.md` | `3c0360b3d9802034fd8da466a1ae376e00ea6095b5dcdfb62ae6e9b2c49b3863` |
| `ux-experience-2026-07-05.md` | `004047aa512d4fa2fbdb10bb5a8fc2d6eec47774c9cb19ad47725c57c1ccf9a7` |

## Overall Verdict

**PASS for document-contract quality at the exact revisions above.** No accessibility-contract defect
was found. The chain is deterministic and testable for every requested review dimension, preserves its
stable identifiers, and clearly separates delivered runtime behavior from new implementation and
evidence work.

**Explicit unresolved Critical/High total: 0.**

This verdict is not implementation conformance or release approval. The contract itself records that
OI-16 and SM-6 remain open until their new deterministic implementation/evidence obligations exist
(`ux-design.md:349-352`). G-4 and D-9/Product approval remain open or pending
(`ux-design.md:353`); this review does not close or claim any of them.

## Severity Summary

| Severity | Unresolved findings |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| **Critical + High** | **0** |

## Strengths

- Announcement ownership is explicit. `Polite status`, the palette-owned combobox status, and the
  focus-only summary path each have one speech owner; the summary deliberately has no live attributes
  or alert role (`ux-design.md:144-150`). Event dedupe retains state/result identity, while the separate
  coalescing-group keys deliberately omit state; the trailing `250` ms boundary, stale-result discard,
  immediate terminal/focused/navigation-failure behavior, and exact sequence/count evidence are all
  testable (`ux-design.md:152-160`).
- FC-NIP correctly separates view-keyed publication from speech dedupe. AM-21 uses
  tenant + user + entity across views, requires a two-view assertion, and makes expiry silent
  (`ux-design.md:114-117`, `184`; `ux-experience-2026-07-05.md:142-145`).
- Validation and rejection are not conflated. VR-01 and VR-02 define stable relationships, declared
  ordering, linked summaries, preserved input, focus, and one speech path; VR-03 keeps an unmapped server
  rejection as a lifecycle outcome with keyboard recovery and no fabricated field error or focus theft
  (`ux-design.md:198-205`).
- Focus is deterministic for route, tab, palette, settings, destructive confirmation, validation,
  rejection, blocked submit, tenant/access replacement, the `/` target, and the in-flow abandonment
  warning (`ux-design.md:207-242`). Origin capture and removed-origin fallbacks are testable, modal
  containment applies only to the actual modal rows, and the in-flow guard remains in page order.
- UX-SS-1 defines its classification subject before the matrix, then covers shell, Home, every FR-11
  projection state, every FR-15 lifecycle state, blocked submit, no-tenant/no-access, forms, overlays,
  the in-flow guard, fresh rows, and filter-hidden detail with all required columns
  (`ux-design.md:244-301`; `ux-experience-2026-07-05.md:101-130`).
- UX-AE-1 and its detailed joins are measurable: real 320 CSS-pixel reflow and 400% browser zoom,
  simultaneous WCAG 1.4.12 spacing values, an all-pointer-target SC 2.5.8 inventory with all five
  standard exceptions and exact spacing/equivalent evidence, complete target-and-indicator
  unobscuration, light/dark/forced-colors behavior, and reduced-motion behavior
  (`ux-design.md:303-319`; `ux-design-detailed-2026-07-05.md:100-112`, `125-162`, `203-212`).
- Support-safe evidence constraints are explicit in the announcement definition, no-tenant/no-access
  rule, and Accessibility Floor (`ux-design.md:138-140`, `160`;
  `ux-experience-2026-07-05.md:199-211`). The chain also correctly states that automated evidence does
  not replace manual assistive-technology or real-device evidence, consistent with
  `docs/accessibility-verification/README.md:41-49`.

## Review Matrix

| Review dimension | Result | Contract/evidence assessment |
| --- | --- | --- |
| One-owner announcements and one speech path | Pass | UX-AM-1 defines the shared polite channel, single palette owner, focus-only summary/heading paths, cancellation rules, and exact-count assertions (`ux-design.md:142-194`). |
| State-free coalescing and event dedupe | Pass | Dedupe includes event state/result; coalescing uses operation/epoch/session keys without state and has exact `249`/`250` ms fake-time behavior (`ux-design.md:152-160`; `ux-experience-2026-07-05.md:134-145`). |
| Fresh rows across views | Pass | Tenant + user + entity speech identity is independent of view; two-view non-reannouncement and silent-expiry evidence are named (`ux-design.md:184`). |
| Validation/rejection and keyboard-only recovery | Pass | VR-01–VR-06 define field/group relationships, focus, preservation, safe mapping distinctions, did-not-run behavior, and keyboard recovery (`ux-design.md:196-205`). |
| Route/tab/palette/dialog/in-flow-guard focus | Pass | FM-01–FM-12 and OF-01–OF-04 name entry, return, fallback, containment, Escape, error destination, unobscured geometry, and acceptance evidence (`ux-design.md:207-242`). |
| Full state-by-surface coverage | Pass | SS-01–SS-49 cover all required state subjects and distinguish terminal attempts/results from non-terminal connection, overlay, dialog, guard, and decoration sessions (`ux-design.md:244-301`). |
| 320 px reflow and 400% zoom | Pass | AE-01/AE-02 require all operations/overlays, logical reading/focus order, no page-level horizontal scroll, and bounded grid/tab-strip scrollers with selected tab/focus visibility; UX-RF-1 and the experience responsive table now state the same rule (`ux-design.md:311-312`; `ux-design-detailed-2026-07-05.md:145-155`; `ux-experience-2026-07-05.md:213-223`). |
| WCAG 1.4.12 text spacing | Pass | AE-03 and UX-TS-1 give all four exact simultaneous values and no-loss conditions, including expansion beyond the default 32 px grid row (`ux-design.md:312`; `ux-design-detailed-2026-07-05.md:125-136`). |
| WCAG 2.5.8 target size | Pass | AE-04 inventories all pointer targets on every changed surface and defines exact proof for Inline, Spacing, Equivalent, User Agent Control, and Essential exceptions (`ux-design.md:313`; `ux-design-detailed-2026-07-05.md:147-162`). |
| Unobscured focus | Pass | AE-05 requires the entire target and indicator to remain visible with chrome, messages, scrolling, drawers, popovers, and dialogs active; FM/RF rows supply per-surface destinations and geometry (`ux-design.md:208-221`, `314`). |
| Light/dark and forced colors | Pass | AE-06/AE-07 and VC-01–VC-09 cover text/non-text/focus contrast and system-color/text/border/icon/shape fallbacks for every semantic state family (`ux-design.md:315-316`; `ux-design-detailed-2026-07-05.md:100-112`). |
| Reduced motion | Pass | AE-08 and RM-1 remove decorative transitions, shimmer, pulse, and smooth scrolling while preserving visible semantics, announcements, focus, and silent fresh-row expiry (`ux-design.md:317`; `ux-design-detailed-2026-07-05.md:203-212`). |
| Delivered behavior versus open work | Pass | The Requirement And Evidence Ledger reports retained behavior and separate implementation/evidence deltas for every scoped FR/NFR (`ux-design.md:321-334`). Representative source checks agree: the current shell permits `HeaderStart` replacement and conditional account rendering (`FrontComposerShell.razor:60-67`, `108-113`), `/` still targets the first active grid filter (`FrontComposerShortcutRegistrar.cs:103-109`, `208-225`), and the current abandonment guard delivers an in-flow Stay/Escape/Leave baseline without deterministic origin return (`FcFormAbandonmentGuard.razor:9-30`; `FcFormAbandonmentGuard.razor.cs:152-200`). These gaps are openly classified, not presented as delivered conformance. |

## Findings

No Critical, High, Medium, or Low document finding remains at the reviewed hashes. Explicitly open
implementation and evidence work is not a document defect because the acceptance behavior, evidence
hooks, and delivery status are complete and testable.

## Gate Boundary

- OI-16: remains open pending the implementation and SM-6 evidence required by the PRD.
- SM-6: remains unmet; this review supplies contract-quality evidence only.
- G-4 and D-9/Product approval: remain open/pending. No Product approval is claimed.
- OI-19/FR-23 full parity: remains independently open and is not treated as closed by this review.
