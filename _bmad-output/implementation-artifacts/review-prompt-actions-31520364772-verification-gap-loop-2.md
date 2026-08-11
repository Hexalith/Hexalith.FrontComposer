Read `/home/administrator/projects/hexalith/frontcomposer/_bmad/render/bmad-build/frontcomposer-2dfd6b67721f/395d43f7f3c70564910e/review-prompts/verification-gap.md` completely and follow it as your review instructions.

Review content:

Complete working-tree diff against baseline `984b459e5cd4fc6d2625cd21f4d8219d4f0f4d1d`:

- `.github/workflows/ci.yml`: reusable Builds CI pin changes from `a8a508...` to `0a3508...`.
- `.github/workflows/release.yml`: all five Builds execution coordinates (`BUILDS_EXECUTION_SHA`, checkout ref, env SHA, reusable workflow `uses`, input SHA) change from `a8a508...` to `0a3508...`.
- `.github/workflows/release-evidence.yml`: Builds checkout ref changes to `0a3508...`.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`: test underscore token count 6360→6359 and its SHA is refreshed.
- `_bmad-output/implementation-artifacts/deferred-work.md`: appends three items with `source_spec: none`: remove duplicate Governance execution from Quality; disable automatic Flaky Test Governance until validated mixed evidence exists; repair red/timing-out nightly benchmark, mutation, and CI-duration workflows.
- `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md`: document that six Hexalith version properties must be present once, canonical/self-default or unconditional, literal NuGet versions, but not point-value mirrored; compatibility comes from authoritative shape plus affected Release/NuGet build; update approved Builds identity to `0a3508...`.
- `_bmad-output/project-context.md`: replaces lockstep local point-value mirror guidance with no `Hexalith*Version` point mirror; retains separate lockstep immutable Release coordinates and updates approved Builds identity to `0a3508...`.
- `references/Hexalith.Builds` gitlink moves `b4e361...`→`0a3508...`; `references/Hexalith.Memories` moves `26bb9b...`→`71cfcb...`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`: removes the second full dependency-graph wrapper test (`PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`), leaving the other canonical invocation.
- `eng/dependency-graph-policy.json`: FrontComposer profile replaces six exact values with sorted `selected_catalog_required_property_names` containing those same six names and an empty `selected_catalog_required_properties` object; required packages and owner checks stay. CI/Release/post-release workflow/action pins, hashes and closure digests are updated for Builds `0a3508...`.
- `eng/dependency_graph.py` adds `_NUGET_VERSION`; adds `selected_catalog_required_property_names` to the closed profile keys; adds `assert_selected_catalog_property_shape` which finds exactly one element, rejects `Choose`/`When`/`Otherwise` ancestors, rejects any conditional ancestor, allows no condition or only the exact canonical self-default condition for the same property, rejects child elements, and requires the element text to match `_NUGET_VERSION`; evaluator applies this list before exact required properties/packages; policy validation requires a nonempty list of valid MSBuild names, ordinal sorted uniqueness, and no overlap with literal required-property keys.
- `tests/eng/test_dependency_graph.py` adds an affected-build regression where restore succeeds, build exits 23, and GraphError must identify module/exit/stderr. It adds policy-schema tests for type, empty, duplicate, unsorted, invalid-name/non-string and overlap rejection. It adds helpers building a synthetic FrontComposer→Builds graph while copying the landed FrontComposer profile and real Builds catalog. It asserts the profile surface is closed and its six names/empty point map exact. For each of the six names independently, it changes only that value to valid `999.0.N` and requires semantic success. It adds failures with owner/path/catalog coordinates for missing, duplicate, empty, `$(OtherVersion)`, nested XML, noncanonical condition, conditional ancestor, and `Choose`; it proves unconditional property passes. It changes exact `ModelContextProtocol.AspNetCore` to `999.0.0` and verifies the targeted expected/found diagnostic.
- New focused spec `spec-actions-31520364772-eliminate-stale-version-mirror-failures.md`: approved frozen intent removes point-value acceptance while retaining graph, identity, structure/ownership, exact packages, delayed activation, affected builds, evidence, handoff; review loop 1 amended non-frozen sections to add the shape-only field/evaluator and all regressions; tasks are checked; results say dependency graph 83, Governance 221, diff check green.
- A separate untracked in-progress spec `spec-align-latest-hexalith-modules-and-simplify-ci.md` describes the broader Builds/Memories/gitlink/workflow update above; its four task boxes remain unchecked and its verification wording mentions four unnamed Builds validators.

Exact new shape function:

```python
def assert_selected_catalog_property_shape(root, prop_name, context):
    matches = list(root.iter(prop_name))
    if len(matches) != 1: raise GraphError(...)
    element = matches[0]
    parents = _parent_map(root)
    ancestors = _ancestors(element, parents)
    if any(node.tag in ("Choose", "When", "Otherwise") for node in ancestors): raise GraphError(...)
    if any(node.get("Condition") is not None for node in ancestors): raise GraphError(...)
    condition = element.get("Condition")
    if condition is not None:
        match = _SELF_DEFAULT_CONDITION.match(condition)
        if match is None or match.group("name") != prop_name: raise GraphError(...)
    observed_text = element.text or ""
    if list(element) or _NUGET_VERSION.fullmatch(observed_text) is None: raise GraphError(...)
```

Focused spec verification already executed successfully: `python3 -m unittest tests/eng/test_dependency_graph.py` 83/83; Release filtered Governance 221/221; `git diff --check`.

Do not invoke any skill. If the instruction file is unreadable, report that exact failure and stop. Return only the review result.
