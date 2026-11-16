# NuGet trusted publishing for FrontComposer

The current production publisher is the repository-owned
`.github/workflows/release-publish.yml` reusable workflow. Its protected job
uses `NuGet/login` to exchange a GitHub OIDC token immediately before Semantic
Release publishes the prepared packages. The temporary key is passed only to
that step; no stored `NUGET_API_KEY` secret is used.

## Publication must stay halted

This caller-owned path is **legacy and not conformant** with the GOV-1
publication-safety boundary (gate G-8). Its protected job checks out and runs
candidate-controlled code while it holds publication authority. The target
architecture is a secretless candidate builder plus a separate protected
publisher that runs only pinned owner code and treats the packages as data.
Architecture has adopted it; Product Owner and Release Owner acceptance is
pending. Production releases must stay halted until that split boundary is
accepted and implemented. The halt is currently not in effect (see below).

`HEXALITH_RELEASE_PUBLISH_ENABLED` is a deny-only stop. Only the exact value
`true` turns publication on mechanically: once its source checks pass, the
prepare step reports `publish-enabled=true`, and the NuGet credential exchange
and Semantic Release run. With any other value the prepare step reports `publish-enabled=false`, and
neither the credential exchange nor publication runs. `true` never authorizes a
release, and neither does a green run nor a production-environment approval.
No bounded risk exception is approved. Keep the variable at literal `false`
while the halt applies.

As of 2026-11-16 the variable reads `true` (updated 2026-10-05), and v4.6.0
through v6.0.0 were published without an approved exception. That is a
recorded nonconformance routed to the Release Owner. It does not authorize
any further release.

## Trusted publishing policy

An active member of the `Hexalith` NuGet organization must sign in to
[nuget.org Trusted Publishing](https://www.nuget.org/account/TrustedPublishing)
and add a GitHub Actions policy with these values:

| Field | Value |
| --- | --- |
| Policy owner | `Hexalith` organization |
| Repository owner | `Hexalith` |
| Repository | `Hexalith.FrontComposer` |
| Workflow file | `release-publish.yml` |
| Environment | `production` |
| Scope | Publish new versions matching `Hexalith.FrontComposer.*` |

The dependency release evaluator uses the policy from the push base commit.
A new `eng/dependency-graph-policy.json` authorization row becomes active for
the following source commit, after that policy commit has landed on `main`.
Before dispatching a release after a policy change, confirm the successful
push CI handoff names the policy commit containing the new row.

The eight existing package IDs are declared in `tools/release-packages.json`.
There is no need to grant publication of new package IDs for this release.
Record the individual **NuGet username that created the policy**, then set the
FrontComposer repository variable `NUGET_USER` to that exact username. It is
not the GitHub actor, an email address, or the `Hexalith` organization name.

A configured policy, a set `NUGET_USER` and green exact-source push CI are
prerequisites only. They do not lift the halt, and turning the variable to
`true` does not authorize a release. Dispatch `release.yml` only from the
current `main` commit; the workflow rejects a source once `main` has advanced.
When it runs, the workflow rechecks the commit and its successful CI run,
prepares and seals the candidate, obtains the temporary NuGet credential when
the variable is exactly `true`, publishes, and checks the GitHub Release.
`release-evidence.yml` independently verifies the published NuGet bytes and
repository signatures. None of these checks makes the legacy path conformant.

See [NuGet's trusted publishing documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
for policy ownership, scopes, and token exchange details.
