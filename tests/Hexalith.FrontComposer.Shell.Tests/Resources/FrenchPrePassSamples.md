# Story 4-1 — French pre-pass sample pack

> **Status:** USER-GATED. Reviewer: Jérôme Piquot (jpiquot@itaneo.com — native French speaker, French-locale company Itaneo).
> Per Story 4-1 D18 / T8.4: SHIP / REVISE / DEFER decision required before merge.

This sample pack renders the three pre-pass-gated French keys against realistic French nouns
(`commandes`, `factures`, `événements`, `aperçu`) — NOT placeholder shells — so the reviewer
sees the user-visible string in production-shape context. If any sample reads grammatically
broken (singular/plural mismatch, gender agreement issue, ICU pluralization required), the
key escalates to Story 9-5 ICU work and the FR translation reverts to identity-pass for v1.

---

## Key 1 — `HomeActionQueueSubtitleTemplate`

**Template (FR):** `{0} {1} en attente d'action`
**English equivalent:** `{0} {1} awaiting your action`
**Render context:** ActionQueue projection subtitle (e.g., orders/invoices awaiting approval).

| Sample | Rendered French | Rendered English |
|---|---|---|
| 0 commandes | `0 commandes en attente d'action` | `0 orders awaiting your action` |
| 1 commande | `1 commandes en attente d'action` ⚠️ | `1 orders awaiting your action` ⚠️ |
| 3 commandes | `3 commandes en attente d'action` | `3 orders awaiting your action` |
| 7 factures | `7 factures en attente d'action` | `7 invoices awaiting your action` |
| 23 factures | `23 factures en attente d'action` | `23 invoices awaiting your action` |

**⚠️ Pluralization concern (singular = 1):** the English copy is also singular-broken (`1 orders`),
so this is a pluralization concern for BOTH locales. Both EN and FR use simple `{0} {1}` formatting
in v1; ICU (`one {entityName}` / `other {entityPlural}`) lands in Story 9-5. Acceptable v1 trade-off,
or escalate immediately?

---

## Key 2 — `HomeDefaultSubtitleTemplate`

**Template (FR):** `{0} {1}`
**English equivalent:** `{0} {1}`
**Render context:** Default projection subtitle (no `[ProjectionRole]` attribute) — minimal "{count} {entityPlural}" format.

| Sample | Rendered French | Rendered English |
|---|---|---|
| 0 commandes | `0 commandes` | `0 orders` |
| 1 commande | `1 commandes` ⚠️ | `1 orders` ⚠️ |
| 12 factures | `12 factures` | `12 invoices` |
| 47 événements | `47 événements` | `47 events` |

**Note:** identity-shape template (`{0} {1}`); reviewer mainly verifies that the placeholder
order matches French expectations (count first, then noun — same as EN).

---

## Key 3 — `HomeEmptyPlaceholderText`

**Template (FR):** `Aucun {0} pour le moment.`
**English equivalent:** `No {0} yet.`
**Render context:** Empty-state placeholder when the projection has no data.

| Sample | Rendered French | Rendered English |
|---|---|---|
| 0 commandes | `Aucun commandes pour le moment.` ⚠️ | `No orders yet.` |
| 0 factures | `Aucun factures pour le moment.` ⚠️ | `No invoices yet.` |
| 0 événements | `Aucun événements pour le moment.` ⚠️ | `No events yet.` |

**⚠️ Gender agreement concern:** `Aucun` is masculine singular. The plural noun + singular adjective
combination (`Aucun commandes`) is grammatically broken in French. Correct French would be either:

1. **Plural agreement**: `Aucune commande pour le moment.` / `Aucun événement pour le moment.` (singular noun + masc/fem singular adjective) — requires the entity-noun to be passed in singular form, which 4-1 doesn't currently support (the generator pluralizes via simple "s" suffix in `FcProjectionEmptyPlaceholder.PluralizeHumanized`).
2. **Plural noun with corrected adjective**: `Aucunes commandes pour le moment.` (plural feminine adjective for plural feminine noun) — requires per-entity gender lookup, ICU-territory.

This key needs an ICU-style pluralization with a singular/plural variant per noun gender. **This is a DEFER candidate for Story 9-5.**

---

## Reviewer decision matrix

For EACH key above, choose ONE of:

- ✅ **SHIP** — render is acceptable as-is; merge with current FR copy.
- 🔄 **REVISE** — suggest a corrected FR string that still fits the `string.Format` pattern (no ICU); update D18 + re-render samples + re-request sign-off, then SHIP.
- ⏸️ **DEFER** — fundamental grammatical issue (singular/plural mismatch, gender agreement, ICU pluralization required). File a Story 9-5 issue referencing the rejected rendering as evidence; revert the FR key to `identity-pass` (FR translation = EN source) so `CanonicalKeysHaveFrenchCounterparts` stays green AND French users see English copy until 9-5 lands ICU.

### Pre-emptive recommendations (Dev Agent assessment)

| Key | Recommendation | Rationale |
|---|---|---|
| `HomeActionQueueSubtitleTemplate` | **SHIP with caveat** | Singular pluralization issue affects EN + FR equally; both fixed in Story 9-5 ICU pass. Current copy reads naturally for typical adopter scenarios (3+ items). |
| `HomeDefaultSubtitleTemplate` | **SHIP** | Identity template; no FR-specific grammar issue. |
| `HomeEmptyPlaceholderText` | **DEFER** | `Aucun {plural-noun}` is broken French. Recommend reverting FR translation to identity-pass (`No {0} yet.` rendered to French users) until Story 9-5 ICU lands gendered pluralization. |

---

## Production rendering reference

The following code reproduces the rendered samples exactly (run `dotnet test --filter FrenchLocaleSubtitleTests` to verify):

```csharp
CultureInfo french = new("fr-FR");
CultureInfo.CurrentCulture = french;
CultureInfo.CurrentUICulture = french;

// HomeActionQueueSubtitleTemplate
string actionQueue = string.Format(french, "{0} {1} en attente d'action", 3, "commandes");
// → "3 commandes en attente d'action"

// HomeDefaultSubtitleTemplate
string defaultSubtitle = string.Format(french, "{0} {1}", 12, "factures");
// → "12 factures"

// HomeEmptyPlaceholderText
string emptyPlaceholder = string.Format(french, "Aucun {0} pour le moment.", "commandes");
// → "Aucun commandes pour le moment."  ⚠️ broken — see DEFER recommendation above
```

---

## Sign-off

- [x] **Reviewer name:** Jérôme Piquot (jpiquot@itaneo.com — native French speaker, Itaneo)
- [x] **Decision per key:**
  - `HomeActionQueueSubtitleTemplate` → ☑ **SHIP** (singular pluralization is bilingual — ICU lands in 9-5)
  - `HomeDefaultSubtitleTemplate` → ☑ **SHIP** (identity template, no FR-specific concern)
  - `HomeEmptyPlaceholderText` → ☑ **DEFER** (gender+number agreement requires ICU; reverted to identity-pass = English source, Known Gap G13)
- [x] **Story 9-5 issue filed for DEFER:** Logged as Known Gap G13 in Story 4-1 file (Story 9-5 owns ICU pluralization implementation; FR translation file currently shows `No {0} yet.` to French users until then).
- [x] **Date of sign-off:** 2026-04-23
