# Epic 12 Context: Adopters Launch an Operations-Ready Domain Shell

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable an adopter to start a FrontComposer operations shell from annotated domain types and demonstrate a generated projection and command through a reproducible, candidate-bound proof kit. The named adopter's independent execution evidence is needed to close the open bootstrap readiness gate; a Product decision may change the obligated adopter without weakening that proof.

## Stories

- Story 12.1: Publish a Deterministic Three-Call Adopter Proof Kit
- Story 12.2: Select a Substitute Adopter

## Requirements & Constraints

- The kit must work from a clean consumer fixture against an exact FrontComposer candidate. Document the supported `AddHexalithFrontComposerQuickstart()`, optional `AddHexalithDomain<TMarker>()`, and `AddHexalithEventStore(...)` sequence and prove that at least one generated projection and one generated command render without repository-private paths, unpublished assumptions, or hand-authored replacement UI.
- Missing or misordered required bootstrap stages must fail before first render with a named diagnostic. An empty domain registry must remain a valid, usable shell state.
- External proof must identify the execution date, candidate SHA, relevant package and runtime identity, projection and command assertions, and pass/fail result. Redact tenant and user data, tokens, payloads, stack traces, and machine-specific paths.
- Hexalith.Tenants is the obligated adopter. Hexalith.Parties becomes the substitute only after a dated Product decision selecting it; a milestone hold leaves the readiness gate open. Either adopter owes the same external proof.
- Reuse existing focused generator, bootstrap, Shell, and authentication regression evidence for already delivered behavior. The kit must not recreate the framework, introduce a separate evidence framework, or claim that internal fixture success is external adopter acceptance.

## Technical Decisions

- Annotated domain types drive generated projection views, command forms, state, registrations, and navigation through Domain Manifest data. Generated output is not hand-edited; the proof should exercise the supported consumer and registration surfaces.
- Keep bootstrap lifetimes valid: scoped authentication, storage, effects, and tenant accessors cannot be captured by singleton services. The shell owns the application frame and account access, including when an adopter customizes the header.
- This is adoption of the brownfield framework. No greenfield starter template is authorized for this epic.
- The proof binds to the candidate being tested. Historical package or runtime identities cannot stand in for the selected candidate's identity.

## UX & Interaction Patterns

- Render the shell through current FrontComposer and pinned Fluent UI Blazor V5 components with Fluent 2 theme roles. Do not add raw replacement controls, a custom theme, legacy Fluent tokens, or new responsive breakpoints.
- Present a bounded context as one Module with a default Module Tab and generated projection and command surfaces. A shell with no registrations says that no modules are available; bootstrap failure presents a safe, focused configuration message rather than appearing to be an empty data result.
- Generated and hand-authored proof surfaces must preserve keyboard access, deterministic focus, meaningful status text, and WCAG 2.2 AA reflow and zoom behavior.

## Cross-Story Dependencies

- Story 12.1 delivers the kit before the selected external adopter maintainer executes it and publishes dated, candidate-bound evidence. FrontComposer records this external dependency but cannot satisfy it on the maintainer's behalf.
- Story 12.2 applies only if Tenants cannot supply proof and the Product Owner invokes the fallback. A dated decision can select Parties or hold the milestone; selection alone does not close the adopter proof gate.
