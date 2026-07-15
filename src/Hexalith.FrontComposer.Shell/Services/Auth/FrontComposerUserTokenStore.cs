using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Process-wide store of per-user access tokens captured at OIDC sign-in. Keyed by the authenticated
/// user's stable identifier (the <c>sub</c>/NameIdentifier claim). It lets a Blazor Server circuit —
/// which has no <see cref="HttpContext"/> — relay the signed-in user's bearer token to a downstream
/// gateway. Tokens are overwritten on each sign-in and removed on sign-out.
/// </summary>
public sealed class FrontComposerUserTokenStore {
    private readonly ConcurrentDictionary<string, StoredToken> _tokens = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="FrontComposerUserTokenStore"/> class.</summary>
    public FrontComposerUserTokenStore(TimeProvider? timeProvider = null)
        => _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>Stores (or overwrites) the access token for the given user id.</summary>
    public void Set(string userId, string accessToken, DateTimeOffset expiresAtUtc) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (expiresAtUtc <= now) {
            RemoveIfExpired(userId, now);
            return;
        }

        _tokens[userId] = new StoredToken(accessToken, expiresAtUtc);
    }

    /// <summary>Reads the stored access token for the given user id, if any.</summary>
    public bool TryGet(string userId, out string accessToken) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (_tokens.TryGetValue(userId, out StoredToken? stored)) {
            if (stored.ExpiresAtUtc > _timeProvider.GetUtcNow()) {
                accessToken = stored.AccessToken;
                return true;
            }

            RemoveExact(userId, stored);
        }

        accessToken = string.Empty;
        return false;
    }

    /// <summary>Removes the stored access token for the given user id (sign-out).</summary>
    public void Remove(string userId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        _ = _tokens.TryRemove(userId, out _);
    }

    private void RemoveIfExpired(string userId, DateTimeOffset now) {
        if (_tokens.TryGetValue(userId, out StoredToken? stored)
            && stored.ExpiresAtUtc <= now) {
            RemoveExact(userId, stored);
        }
    }

    private void RemoveExact(string userId, StoredToken token)
        => ((ICollection<KeyValuePair<string, StoredToken>>)_tokens)
            .Remove(new KeyValuePair<string, StoredToken>(userId, token));

    private sealed class StoredToken {
        public StoredToken(string accessToken, DateTimeOffset expiresAtUtc) {
            AccessToken = accessToken;
            ExpiresAtUtc = expiresAtUtc;
        }

        public string AccessToken { get; }
        public DateTimeOffset ExpiresAtUtc { get; }
    }
}
