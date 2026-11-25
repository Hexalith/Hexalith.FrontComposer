---
title: 'Deferred EventStore boundaries and authentication help'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next six actionable ledger entries after DW-919. Skip blocked and superseded entries. Verify the resulting behavior, record each resolution, and commit each item separately.

</frozen-after-approval>

## Implemented

- DW-920 shares the Shell and EventStore tenancy registrations and retains adopter overrides.
- DW-921 checks the captured query scope immediately before HTTP dispatch and reports the correlation ID recorded by revalidation.
- DW-922 checks the original active registration at callback dispatch. A callback still in revalidation cannot target a replacement subscription for the same group.
- DW-925 maps nonfatal identity accessor failures to the sanitized missing-context result and preserves cancellation.
- DW-929 links every authentication validation diagnostic to a guide shipped in the repository and included in DocFX.
- DW-933 exposes the selected authentication provider as a read-only options property and keeps startup validation for ambiguous configuration.

## Skipped

- DW-926 requires a policy for changing the connection settings and captured identity of a live subscription.
- DW-927 requires preserving the token freshness and captured-session checks at SignalR dispatch before reusing the preflight result.
- DW-928 is covered by the guarded transport token callback during automatic reconnect and the preflight on explicit restart.
- DW-930's archived story-creation lessons cover planning and review process. The entry needs specific observable behaviors or workflow checks before it can be implemented as test coverage.
- DW-931 requires choosing how a scoped token provider can reach EventStore consumers now configured through singleton options.
- DW-932 needs a broker and SAML handler contract defining who owns callback state and correlation validation.

## Review Triage Log

- DW-927 duplicate fetch, medium, deferred under the existing ledger entry. The transport callback independently revalidates session and token freshness after preflight.
- Dispatch interleaving, low, deferred. The new registration check is the admission point. A callback admitted just before retirement can still run. Waiting for all callbacks would require a reentrancy policy for subscribers that unsubscribe themselves.
- Detail notifier fan-out, low, deferred with dispatch interleaving. The existing notifier admits one delivery and then awaits individual subscribers. Retirement during fan-out leaves that delivery in flight.
- Query diagnostic correlation, medium, fixed. Exceptions now carry the same correlation ID as the rejection warning, with a regression assertion.
- DW-933 scope, false as an implementation concern. The request delegates selection of the sixth actionable item when earlier work is blocked. The initial working draft named DW-927 before the token check was inspected.
- SAML token provider requirement, medium, fixed. The guide states that authentication registration requires the host token provider.
- Claim alias constraints, low, fixed. The guide requires nonempty lists made up entirely of nonblank claim names.
- Custom broker challenge handler, medium, fixed. The guide documents handler registration and reuse of an existing host scheme.
- Help anchors, low, verified. Validator links target the GitHub-rendered guide. The guide's headings are also rendered by DocFX with all 15 diagnostic anchors.
