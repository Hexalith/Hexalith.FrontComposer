# PRD quality review — November 20 update

## Overall verdict

The revised requirements address F01–F13. Each of the five high findings now has a source-backed correction in the relevant gate or feature. The Release Owner can follow the November 19 stop receipt and publisher design, and adopters can identify the supported runtime and local tools. One cross-reference in the requirement status map still labels accepted evidence as open work in the reviewed snapshot.

## Decision-readiness — strong

Section 0.1 names the evidence and owners for the open G-1, G-2, G-3, G-4 and G-8 gates. G-3 records the stopped preflight for `ffb75718`, the CiCd decision and the record ref for any separately authorized subsequent candidate. G-8 distinguishes the completed source reconciliation and reviewed runbook from the remaining publisher implementation, preservation evidence and owner acceptance. The two dated emergency-stop observations agree with NC-35.

## Substance over theater — strong

FR-15 distinguishes acknowledgement from confirmed completion. FR-20 gives the adopter a packaged CLI check against two real Counter types. The check exercises behavior that the previous flat-output fixture missed.

## Strategic coherence — strong

Nina's bootstrap journey and SM-1 test adoption of a generated domain shell. Marc's projection journey and SM-7 test the reliability of the visible result. The release counter-metrics preserve the separate evidence and owner decisions required by FR-24.

## Done-ness clarity — strong

FR-2 now gives both an accepted default-argument constructor and a rejected required-argument constructor. FR-20 identifies the Counter command and projection in the packaged smoke test. FR-21 specifies per-file atomicity, persistence of earlier writes, exit code 4 for handled failures, and the reporting limit on cancellation.

## Scope honesty — strong

Sections 5.5, 5.6 and 11 identify the retained local compiler commands and distinguish them from the MCP host. The latter retains its runtime obligations until all consumers are inventoried and the Projects plug-in, host and dependent acceptance evidence are ready together. G-4 preserves the Product decision for Fluent 5.1.0 after crediting the documentation correction.

## Downstream usability — adequate

Sections 6, 7 and 11 align the SDK, consumer runtimes and netstandard assets with the selected project files and the 6.0 migration guide. FR-25 and OI-11 align the 5.0.0 executable comparison baseline with the historical owner decision. The status map also needs to carry forward the candidate limits in §0.1 when a story author extracts it separately.

### Finding

- **[medium]** Section 5.0 labels G-6 and G-7 as open. Table A is headed “work still gated” and its “Open gate” column includes both gates, although §0.1 records dated acceptance for each. Table B also describes FR-19 host enforcement as outstanding Table A work. *Fix:* Mark those two evidence rows as accepted for their named candidates and identify FR-19 enforcement as a regression obligation. The parent update is addressing this finding before final synthesis.

## Shape fit — adequate

A capability specification with bounded adopter and security evidence fits a brownfield framework that serves operators, developers and release owners. The current G-3 rule is visible in the gate table, together with links to the November amendment and the later stop decision. The historical appendices retain the original decision context.

## Mechanical notes

The revised PRD defines FR-1 through FR-30 plus FR-19a. The six journeys still identify their protagonists. The A3 assumption links the sample-host statement to OI-9. Both documents carry the November 20 revision date. Source verification used the checked-in requirements, CLI smoke test, migration guide, SDK and package configuration, architecture decision, incident runbook, and dated evidence. Product re-approval remains pending.

Pre-polish snapshot findings total 0 critical, 0 high, 1 medium and 0 low. The final synthesis records the disposition on the delivered bytes.

## Post-polish disposition — 2026-11-20

**Resolved.** Section 5.0 Table A records G-6 acceptance on the named Tenants candidate and G-7 acceptance on the v2 MCP manifest. Table B places FR-19 framework protections in regression coverage. The map preserves the deployment owner's responsibilities.

After the editorial changes, §0.1 still records G-1, G-2, G-3, G-4 and G-8 as open and G-5, G-6 and G-7 as closed for their stated evidence. OI-4, OI-16 and OI-19 remain G-4 prerequisites. G-3 still requires candidate-specific qualification and approvals, and G-8 retains the publication halt and outstanding conformance decisions. Product re-approval is pending.

Final unresolved findings total 0 critical, 0 high, 0 medium and 0 low. SHA-256 values below cover the exact working-tree file bytes.

- PRD · `656a1f6777d16f0af2a038071d24b47843cfe14a7de694d865d7022c436b51d6`
- Addendum · `9604e2c70f1d4b63150b7fec7771ad2acebe8ca051b509a3b76d880aa00a0927`
