# Counter FrontComposer Auth Notes

The Counter sample keeps `DemoUserContextAccessor` as the default so local development runs without external identity provider credentials.

To smoke-test an authenticated-looking sample context without live OIDC/SAML infrastructure, set:

```json
{
  "Hexalith": {
    "FrontComposer": {
      "FakeAuth": {
        "Enabled": true
      }
    }
  }
}
```

Production hosts should replace the demo/fake accessor with the Story 7-1 bridge:

```csharp
builder.Services.AddHexalithFrontComposerQuickstart(...);
builder.Services.AddHexalithFrontComposerAuthentication(options =>
{
    options.UseKeycloak(
        authority: new Uri("https://identity.example.com/realms/frontcomposer"),
        clientId: "frontcomposer",
        clientSecret: builder.Configuration["Auth:ClientSecret"]
            ?? throw new InvalidOperationException("Auth:ClientSecret is required."),
        tenantClaimType: "tenant_id",
        userClaimType: "sub");
});
builder.Services.AddHexalithEventStore(options =>
{
    options.BaseAddress = new Uri("https://eventstore.example.com");
});
```

Keep provider secrets in user secrets or deployment secret storage. Do not put real client secrets, tokens, tenant IDs, user IDs, SAML assertions, or provider payloads in sample configuration.
