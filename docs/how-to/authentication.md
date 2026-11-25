---
title: "Configure authentication"
description: "Configure FrontComposer identity providers and resolve authentication startup diagnostics."
genre: how-to
audience: adopter
ownerStory: 7-1-oidc-saml-authentication-integration
status: published
reviewed: 2026-11-25
uid: frontcomposer.how-to.authentication
slug: how-to/authentication/
---

# Configure authentication

Call `AddHexalithFrontComposerAuthentication` to register the authentication provider in the host. FrontComposer validates the provider, the tenant and user claim mappings, and the source of the EventStore bearer token during startup.

## Single provider

Enable exactly one of `OpenIdConnect`, `Saml2`, `GitHubOAuth`, or `CustomBrokered`. Configure the host authentication handler for the selected mode and disable the others.

For `CustomBrokered`, use `CustomBrokered.ConfigureAuthentication` to register a handler under `CustomBrokered.ChallengeScheme`, or use the scheme of a handler that the host already registers. Set `TokenRelay.HostAccessTokenProvider` to the broker's server-side bearer-token provider.

## OIDC authority

Set `OpenIdConnect.Authority` to the provider's absolute discovery authority, or supply `OpenIdConnect.MetadataAddress`. Use HTTPS outside Development and keep credentials out of the URI. If you set both values, use the same host for both.

## OIDC metadata

Set `OpenIdConnect.MetadataAddress` to the absolute URL of the provider discovery document when overriding the default discovery address. Use HTTPS outside Development and keep credentials out of the URI.

## OIDC client

Set `OpenIdConnect.ClientId` to the application registration's client ID. Store client secrets in the host's secret store.

## OIDC audience

Set `OpenIdConnect.Audience` to the expected token audience if the deployment requires an explicit audience. Keep the value null when the provider's default applies.

## OIDC response type

Set `OpenIdConnect.ResponseType` to `code` for the server-side authorization code flow.

## OIDC scopes

Include `openid` in `OpenIdConnect.Scopes`. Add `profile` when the host uses profile claims.

## SAML handler

Set `Saml2.ConfigureHandler` to register the host's SAML handler. Set `TokenRelay.HostAccessTokenProvider` to the host’s brokered bearer-token provider. Authentication registration requires this option for SAML.

## SAML metadata

Set `Saml2.MetadataAddress` to an absolute identity-provider metadata URL when supplying metadata. Use HTTPS outside Development and register the handler through `Saml2.ConfigureHandler`.

## GitHub credentials

Set `GitHubOAuth.ClientId` and `GitHubOAuth.ClientSecret` from the OAuth app registration. Load the secret through host-owned secret storage.

## GitHub broker

Use a host broker and set `TokenRelay.HostAccessTokenProvider` to return a bearer token accepted by EventStore. GitHub sign-in identifies the user, while EventStore validates the bearer token against its own authority and audience.

## Token relay

Set `TokenRelay.AccessTokenName` to the name used by the registered authentication handler. The default is `access_token`. For OIDC Blazor Server hosts, use `AddHexalithFrontComposerServerSecurity` for the circuit-bound token source. For brokered hosts, set `TokenRelay.HostAccessTokenProvider` to the host's server-side bearer-token provider.

## Claim aliases

Supply at least one claim type in each of `TenantClaimTypes` and `UserClaimTypes`. Every entry must be a nonblank claim name. Use the claim names issued by the host's identity provider and list each alias only once.

## Scheme collision

Use different values for the cookie sign-in scheme and the provider challenge scheme. Match the names registered by the host's authentication handlers.

## Callback path

Start callback and signed-out callback paths with `/`, such as `/signin-oidc`. Register the corresponding application callback URLs with the identity provider.
