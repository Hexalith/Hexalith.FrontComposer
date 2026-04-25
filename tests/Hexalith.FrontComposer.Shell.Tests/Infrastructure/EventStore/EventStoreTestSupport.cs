using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 — shared helpers so existing client/cancellation/diagnostics tests can satisfy
/// the new <c>EventStoreResponseClassifier</c> + <c>IETagCache</c> constructor parameters
/// without each test fixture having to spin its own copy.
/// </summary>
internal static class EventStoreTestSupport {
    /// <summary>Returns a ready-to-use classifier wired with a null logger.</summary>
    public static EventStoreResponseClassifier CreateClassifier()
        => new(NullLogger<EventStoreResponseClassifier>.Instance);

    /// <summary>
    /// A no-op <see cref="IETagCache"/> that always reports "no readable cache key" — used
    /// by tests that pre-date Story 5-2 cache integration so they exercise the fresh-fetch
    /// path exclusively.
    /// </summary>
    public sealed class NoCache : IETagCache {
        public bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key) {
            key = string.Empty;
            return false;
        }

        public Task<ETagCacheEntry?> TryGetAsync(string key, int expectedPayloadVersion, CancellationToken cancellationToken = default)
            => Task.FromResult<ETagCacheEntry?>(null);

        public Task SetAsync(string key, ETagCacheEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    /// <summary>A no-op redirector used by query tests that need to observe the typed 401 exception.</summary>
    public sealed class RecordingAuthRedirector : IAuthRedirector {
        public int CallCount { get; private set; }

        public Task RedirectAsync(string? returnUrl = null, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
