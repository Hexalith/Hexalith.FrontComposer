using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Process-wide store of access tokens bound to a server-generated sign-in session, stable subject
/// and current Tenant. Circuits share credentials only within that exact authenticated session.
/// Sign-out revokes that session; every read checks expiry.
/// </summary>
public sealed class FrontComposerUserTokenStore {
    private readonly ConcurrentDictionary<FrontComposerTokenSession, StoredToken> _tokens = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="FrontComposerUserTokenStore"/> class.</summary>
    public FrontComposerUserTokenStore(TimeProvider? timeProvider = null)
        => _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>Gets the current time from the credential store clock.</summary>
    internal DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    /// <summary>Refuses unscoped writes; use the overload accepting <see cref="FrontComposerTokenSession"/>.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Preserves the published instance API while refusing credentials without session authority.")]
    public void Set(string userId, string accessToken, DateTimeOffset expiresAtUtc)
        => throw new InvalidOperationException("Token credentials require a FrontComposerTokenSession binding.");

    /// <summary>Cannot resolve credentials without a session and Tenant; always returns false.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Preserves the published instance API while refusing credentials without session authority.")]
    public bool TryGet(string userId, out string accessToken)
    {
        accessToken = string.Empty;
        return false;
    }

    /// <summary>Refuses unscoped revocation; use the overload accepting <see cref="FrontComposerTokenSession"/>.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Preserves the published instance API while refusing credentials without session authority.")]
    public void Remove(string userId)
        => throw new InvalidOperationException("Token credentials require a FrontComposerTokenSession binding.");

    /// <summary>Stores (or overwrites) the access token for the exact session binding.</summary>
    public void Set(FrontComposerTokenSession session, string accessToken, DateTimeOffset expiresAtUtc) {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        // Unique sign-ins must not retain expired bearer strings from abandoned sessions forever.
        foreach ((FrontComposerTokenSession key, StoredToken stored) in _tokens)
        {
            if (stored.ExpiresAtUtc <= now)
            {
                RemoveExact(key, stored);
            }
        }

        if (expiresAtUtc <= now) {
            RemoveIfExpired(session, now);
            return;
        }

        _tokens[session] = new StoredToken(accessToken, expiresAtUtc);
    }

    /// <summary>Reads the stored access token for the exact session binding, if any.</summary>
    public bool TryGet(FrontComposerTokenSession session, out string accessToken) {
        ArgumentNullException.ThrowIfNull(session);

        if (_tokens.TryGetValue(session, out StoredToken? stored)) {
            if (stored.ExpiresAtUtc > _timeProvider.GetUtcNow()) {
                accessToken = stored.AccessToken;
                return true;
            }

            RemoveExact(session, stored);
        }

        accessToken = string.Empty;
        return false;
    }

    /// <summary>Removes the stored access token for the exact session binding (sign-out).</summary>
    public void Remove(FrontComposerTokenSession session) {
        ArgumentNullException.ThrowIfNull(session);
        _ = _tokens.TryRemove(session, out _);
    }

    /// <summary>Revokes the entire sign-in even when its current Tenant claims have changed.</summary>
    internal void RemoveSession(string sessionId)
    {
        foreach ((FrontComposerTokenSession session, StoredToken stored) in _tokens)
        {
            if (string.Equals(session.SessionId, sessionId, StringComparison.Ordinal))
            {
                RemoveExact(session, stored);
            }
        }
    }

    private void RemoveIfExpired(FrontComposerTokenSession session, DateTimeOffset now) {
        if (_tokens.TryGetValue(session, out StoredToken? stored)
            && stored.ExpiresAtUtc <= now) {
            RemoveExact(session, stored);
        }
    }

    private void RemoveExact(FrontComposerTokenSession session, StoredToken token)
        => ((ICollection<KeyValuePair<FrontComposerTokenSession, StoredToken>>)_tokens)
            .Remove(new KeyValuePair<FrontComposerTokenSession, StoredToken>(session, token));

    private sealed class StoredToken {
        public StoredToken(string accessToken, DateTimeOffset expiresAtUtc) {
            AccessToken = accessToken;
            ExpiresAtUtc = expiresAtUtc;
        }

        public string AccessToken { get; }
        public DateTimeOffset ExpiresAtUtc { get; }
    }
}
