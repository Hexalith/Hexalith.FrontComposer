namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Read-only accessor that surfaces the current tenant and user identifiers. Adopters register an
/// implementation that bridges their authentication stack (e.g. <c>IHttpContextAccessor</c> claims
/// in Blazor Server, <c>AuthenticationStateProvider</c> in WASM, or a demo stub in the Counter sample).
/// </summary>
/// <remarks>
/// Decision D31 (fail-closed) REQUIRES non-null AND non-whitespace <see cref="TenantId"/> +
/// <see cref="UserId"/> before <c>LastUsedValueProvider</c> reads or writes. Returning
/// <see langword="null"/>, an empty string, or a whitespace-only string from this accessor is
/// the sanctioned way to express "no authenticated context"; the provider then no-ops and
/// publishes a single dev-mode diagnostic. Implementations MUST treat null, empty, and
/// whitespace as semantically equivalent — do not return <c>"   "</c> when you mean
/// "unauthenticated."
/// </remarks>
public interface IUserContextAccessor {
    /// <summary>
    /// Gets the current tenant identifier. Implementations return <see langword="null"/>, an
    /// empty string, or whitespace when there is no authenticated tenant context; consumers
    /// treat all three as "unauthenticated" via <see cref="string.IsNullOrWhiteSpace(string?)"/>.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the current user identifier. Implementations return <see langword="null"/>, an
    /// empty string, or whitespace when there is no authenticated user context; consumers
    /// treat all three as "unauthenticated" via <see cref="string.IsNullOrWhiteSpace(string?)"/>.
    /// </summary>
    string? UserId { get; }
}
