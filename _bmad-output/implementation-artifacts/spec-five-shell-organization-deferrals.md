---
title: 'Harden Shell source-organization governance'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'be9b965ab1e7dc2eb144872e746f5143465864a1'
context: []
---

<frozen-after-approval reason="Five open findings from the Shell organization guard">

## Intent

Close DW-1702, DW-1704, DW-1705, DW-1711, and DW-1712. Keep the one-handwritten-file rule for each Shell type, including partial types. Count same-shape partial declarations in mutually exclusive branches separately. For a single declaration with conditional modifiers, use a shape from an actual preprocessor configuration while retaining the scan for unreachable declarations. Distinguish a compiled record struct from an ordinary struct with a hand-written `PrintMembers` method, and verify that only the complete `partial` token permits collapsing declarations. Exercise the cases in the existing Shell governance class.

</frozen-after-approval>

## Implementation Notes

The Shell guard now enforces one handwritten file for each shared type identity. The two feedback components each had byte-identical partial stubs in two files. Their Razor code-behind files now carry the declarations and summaries. The compiled XML contains the same documentation after removing the two duplicate summaries.

The guard selects modifier shapes from observed Roslyn configurations. It uses identifier positions to check whether compatible partial fragments can coexist and parses unreachable arms separately to preserve declaration scopes. Record struct reflection uses the generated equality operator, while the source pins still check the exact declaration syntax.

The preceding main revision already had 3,617 public test identifiers in the CA1707 inventory against a seal of 3,596. The final inventory has 3,627. The change adds ten declarations and renames two. The seal now records the measured count and hash, with the analyzer policy and Contracts seal unchanged.

## Review Triage Log

- Medium, fixed conditional partial matching by comparing physical identifier positions. Compatible fragments can coexist in different pairs of configurations.
- Medium, fixed cross-file identity checks by normalizing namespace identifier tokens and considering whether a declaration can be shared in any observed configuration.
- Medium, fixed unreachable-branch parsing by isolating competing arms. Results include declarations whose containing namespace or type changes outside the directive block, and isolated parses preserve compatible partial fragments in the same scope.
- Low, retained the runtime compiler-generated equality check. A hand-written operator can carry the same attribute, so the independent source pin verifies the declared record kind for the pinned surface.

## Verification

The added probes failed in five cases against the original guard. The final Shell Release build used .NET SDK 11.0.100 and the committed Builds catalog at `6439a71d240a32a7486c4fd437539a56c7a909b8` and reported zero warnings and errors.

Direct xUnit execution passed all 48 cases in `ShellTypeOrganizationGovernanceTests`. The broader non-contract Shell run passed 4,050 cases with zero failures or skips, including the analyzer inventory seal.
