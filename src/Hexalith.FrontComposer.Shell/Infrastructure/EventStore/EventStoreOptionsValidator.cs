using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal sealed class EventStoreOptionsValidator : IValidateOptions<EventStoreOptions> {
    public ValidateOptionsResult Validate(string? name, EventStoreOptions options) {
        if (options.BaseAddress is null) {
            return ValidateOptionsResult.Fail("EventStore BaseAddress is required.");
        }

        if (!options.BaseAddress.IsAbsoluteUri) {
            return ValidateOptionsResult.Fail("EventStore BaseAddress must be absolute.");
        }

        if (!IsPath(options.CommandEndpointPath)) {
            return ValidateOptionsResult.Fail("CommandEndpointPath must start with '/'.");
        }

        if (!IsPath(options.QueryEndpointPath)) {
            return ValidateOptionsResult.Fail("QueryEndpointPath must start with '/'.");
        }

        if (!IsPath(options.ProjectionChangesHubPath)) {
            return ValidateOptionsResult.Fail("ProjectionChangesHubPath must start with '/'.");
        }

        if (options.Timeout <= TimeSpan.Zero) {
            return ValidateOptionsResult.Fail("Timeout must be positive.");
        }

        if (options.MaxETagCount <= 0 || options.MaxETagCount > 10) {
            return ValidateOptionsResult.Fail("MaxETagCount must be between 1 and 10.");
        }

        if (options.MaxRequestBytes <= 0) {
            return ValidateOptionsResult.Fail("MaxRequestBytes must be positive.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsPath(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return false;
        }

        if (!value.StartsWith("/", StringComparison.Ordinal)) {
            return false;
        }

        // Reject path-network-form (//), query/fragment, and any embedded whitespace or control characters.
        if (value.Length > 1 && value[1] == '/') {
            return false;
        }

        for (int i = 0; i < value.Length; i++) {
            char ch = value[i];
            if (char.IsControl(ch) || char.IsWhiteSpace(ch) || ch == '?' || ch == '#') {
                return false;
            }
        }

        return Uri.TryCreate(value, UriKind.Relative, out _);
    }
}
