using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Options;

/// <summary>
/// Story 5-2 D2 — the new <see cref="FcShellOptions.MaxETagCacheEntries"/> option must be
/// bounded above by <see cref="FcShellOptions.LocalStorageMaxEntries"/> so a busy ETag cache
/// cannot evict unrelated persisted preference state.
/// </summary>
public class Story52FcShellOptionsValidatorTests {
    [Fact]
    public void Validator_AcceptsDefaults() {
        FcShellOptionsThresholdValidator validator = new();
        FcShellOptions options = new();

        ValidateOptionsResult result = validator.Validate(name: null, options);

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validator_AcceptsCacheDisabled_Zero() {
        FcShellOptionsThresholdValidator validator = new();
        FcShellOptions options = new() { MaxETagCacheEntries = 0 };

        ValidateOptionsResult result = validator.Validate(name: null, options);

        result.Failed.ShouldBeFalse();
    }

    [Fact]
    public void Validator_RejectsCacheCapAboveLocalStorageCap() {
        FcShellOptionsThresholdValidator validator = new();
        FcShellOptions options = new() {
            LocalStorageMaxEntries = 100,
            MaxETagCacheEntries = 200,
        };

        ValidateOptionsResult result = validator.Validate(name: null, options);

        result.Failed.ShouldBeTrue();
        result.Failures.ShouldContain(message => message.Contains("MaxETagCacheEntries"));
    }
}
