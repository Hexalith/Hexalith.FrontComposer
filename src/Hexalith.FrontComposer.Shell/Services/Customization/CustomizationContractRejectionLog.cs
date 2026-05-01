using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Hexalith.FrontComposer.Shell.Services.Customization;

/// <summary>
/// Default thread-safe implementation of <see cref="ICustomizationContractRejectionLog"/>.
/// Story 6-6 P17 / AC2.
/// </summary>
internal sealed class CustomizationContractRejectionLog : ICustomizationContractRejectionLog {
    private readonly ConcurrentBag<CustomizationContractRejection> _entries = [];

    /// <inheritdoc />
    public IReadOnlyList<CustomizationContractRejection> Rejections => _entries.ToArray();

    /// <inheritdoc />
    public void Record(CustomizationContractRejection rejection) {
        ArgumentNullException.ThrowIfNull(rejection);
        _entries.Add(rejection);
    }
}
