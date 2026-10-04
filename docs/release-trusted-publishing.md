# NuGet trusted publishing for FrontComposer

The production publisher is the repository-owned
`.github/workflows/release-publish.yml` reusable workflow. Its protected job
uses `NuGet/login` to exchange a GitHub OIDC token immediately before Semantic
Release publishes the prepared packages. The temporary key is passed only to
that step; no stored `NUGET_API_KEY` secret is used.

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

The eight existing package IDs are declared in `tools/release-packages.json`.
There is no need to grant publication of new package IDs for this release.
Record the individual **NuGet username that created the policy**, then set the
FrontComposer repository variable `NUGET_USER` to that exact username. It is
not the GitHub actor, an email address, or the `Hexalith` organization name.

Keep `HEXALITH_RELEASE_PUBLISH_ENABLED` set to `false` until the policy is
active, `NUGET_USER` is set, and exact-source push CI has passed. Then set it
to `true` and dispatch `release.yml` from the current `main` commit. The
workflow rechecks that commit and its successful CI run, prepares and seals
the candidate, obtains the temporary NuGet credential, publishes, and checks
the GitHub Release. `release-evidence.yml` independently verifies the
published NuGet bytes and repository signatures.

See [NuGet's trusted publishing documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
for policy ownership, scopes, and token exchange details.
