# Epic 7: Authentication, Authorization & Multi-Tenancy

Users authenticate via OIDC/SAML (Keycloak, Entra ID, GitHub, Google), commands are authorized via declarative policy attributes, and tenant context from JWT is propagated and enforced across all command, query, and subscription operations.

### Story 7.1: OIDC/SAML Authentication Integration

As a developer,
I want to integrate standard identity providers via OIDC/SAML without building a custom authentication UI,
So that users can authenticate with their existing corporate or social identity and I don't maintain auth UI code.

**Acceptance Criteria:**

**Given** the framework's authentication configuration
**When** an identity provider is configured
**Then** standard OIDC/SAML flows are used for authentication
**And** the following identity providers are supported: Keycloak, Microsoft Entra ID, GitHub, and Google
**And** no custom authentication UI is shipped by the framework (all auth UI comes from the identity provider)

**Given** a user navigates to the application unauthenticated
**When** the authentication flow triggers
**Then** the user is redirected to the configured identity provider's login page
**And** on successful authentication, the user is redirected back with a JWT bearer token
**And** the JWT is stored and propagated through all subsequent requests

**Given** a JWT bearer token
**When** the token is inspected by the framework
**Then** TenantId and UserId claims are extracted
**And** the token is validated (signature, expiry, audience)
**And** failed validation redirects to re-authentication

**Given** v1 scope
**When** authentication is configured
**Then** a single identity provider is configured per deployment
**And** multi-IdP support (simultaneous Keycloak + Entra ID) is documented as a v1.x enhancement

**Given** zero PII requirements (NFR102)
**When** the framework processes JWT tokens
**Then** only TenantId and UserId claims are extracted for framework operations
**And** no PII from the token is stored, logged, or cached at the framework layer

**Given** JWT token storage requirements
**When** the authentication state is managed
**Then** the standard ASP.NET Core authentication state provider pattern is used (not raw LocalStorage)
**And** Blazor Server uses server-side circuit state (no client-side token storage)
**And** Blazor WebAssembly uses the framework's secure token handling with HttpOnly cookie preference where possible
**And** raw JWT tokens are never stored in LocalStorage (XSS mitigation)

**References:** FR37, NFR20, NFR21, NFR102

---

### Story 7.2: Tenant Context Propagation & Isolation

As a business user,
I want my data to be completely isolated from other tenants with no possibility of cross-tenant data leakage,
So that I can trust the application with my organization's data.

**Acceptance Criteria:**

**Given** an authenticated user with a JWT containing TenantId claim
**When** the user performs any operation
**Then** TenantId is propagated through all command dispatch operations (included in command envelope)
**And** TenantId is propagated through all query execution operations (included in query parameters)
**And** TenantId is included in SignalR group subscriptions ({projectionType}:{tenantId})
**And** TenantId scopes all ETag cache keys ({tenantId}:{userId}:{featureName}:{discriminator})
**And** TenantId scopes MCP tool enumeration (agents see only tools for their tenant)

**Given** the tenant isolation enforcement
**When** a request attempts to access data from a different tenant
**Then** the framework layer blocks the request before it reaches the backend
**And** cross-tenant data visibility is treated as a security bug (NFR28)
**And** the violation is logged at Error level with TenantId, attempted TenantId, and CorrelationId

**Given** DAPR actor key patterns
**When** actor IDs are constructed
**Then** the pattern {projectionType}:{tenantId} is used
**And** no colons appear in ProjectionType or TenantId values (DAPR actor ID separator constraint)

**Given** the IStorageService cache
**When** cache entries are scoped
**Then** all entries include {tenantId}:{userId} prefix
**And** cache lookup never returns entries from a different tenant, even if UserId matches

**Given** v0.1 scope (single-tenant)
**When** multi-tenancy is not yet configured
**Then** a stub TenantProvider returns a fixed default tenant ID
**And** all framework operations function correctly with the stub
**And** the stub is replaceable with the real JWT-based provider without code changes (only configuration)

**References:** FR35, NFR21, NFR22, NFR28, Architecture Multi-Tenancy

---

### Story 7.3: Command Authorization Policies

As a developer,
I want to apply authorization policies to commands via declarative attributes that integrate with ASP.NET Core,
So that I can enforce role-based and policy-based access control on domain operations using the standard .NET authorization model.

**Acceptance Criteria:**

**Given** a command annotated with [RequiresPolicy("OrderApprover")]
**When** a user without the "OrderApprover" policy submits the command
**Then** the framework rejects the command before dispatch to the backend
**And** a 403 Forbidden response is returned
**And** the UX renders: "You don't have permission to [command action]" via FluentMessageBar (Warning)

**Given** a command annotated with [RequiresPolicy]
**When** the authorization check executes
**Then** it integrates with ASP.NET Core authorization middleware
**And** standard IAuthorizationService is used for policy evaluation
**And** claims from the JWT bearer token are available for policy evaluation

**Given** a command with a [RequiresPolicy] attribute referencing a policy name
**When** the referenced policy is not registered in the authorization configuration
**Then** a build-time warning is emitted: "Policy '{policyName}' referenced by [RequiresPolicy] on {CommandType} is not registered. See HFC{id}."
**And** the warning includes a documentation link for policy registration

**Given** commands without [RequiresPolicy] attributes
**When** they are submitted
**Then** no authorization check is performed beyond basic authentication (user must be authenticated)
**And** this is the default behavior for commands that don't need role/policy restrictions

**Given** the authorization layer
**When** inline action buttons render on DataGrid rows for policy-protected commands
**Then** buttons for commands the user is not authorized to execute are hidden or disabled
**And** the authorization check uses the same policy evaluation as the dispatch check (no divergence between UI and backend)

**References:** FR46, NFR23, Architecture Security

---

**Epic 7 Summary:**
- 3 stories covering all 3 FRs (FR35, FR37, FR46)
- Relevant NFRs woven into acceptance criteria (NFR20-23, NFR28, NFR102)
- v0.1 single-tenant stub included for backward compatibility
- Stories are sequentially completable: 7.1 (authentication) -> 7.2 (tenant propagation) -> 7.3 (authorization policies)

---
