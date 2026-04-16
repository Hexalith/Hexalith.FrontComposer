namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Read-only accessor that surfaces the current tenant and user identifiers. Adopters register an
/// implementation that bridges their authentication stack (e.g. <c>IHttpContextAccessor</c> claims
/// in Blazor Server, <c>AuthenticationStateProvider</c> in WASM, or a demo stub in the Counter sample).
/// </summary>
/// <remarks>
/// Decision D31 (fail-closed) REQUIRES non-empty <see cref="TenantId"/> + <see cref="UserId"/> before
/// <c>LastUsedValueProvider</c> reads or writes. Returning <see langword="null"/> / empty from this
/// accessor is the sanctioned way to express "no authenticated context"; the provider then no-ops
/// and publishes a single dev-mode diagnostic.
/// </remarks>
public interface IUserContextAccessor {
    /// <summary>Gets the current tenant identifier, or <see langword="null"/> when unauthenticated / unknown.</summary>
    string? TenantId { get; }

    /// <summary>Gets the current user identifier, or <see langword="null"/> when unauthenticated / unknown.</summary>
    string? UserId { get; }
}
