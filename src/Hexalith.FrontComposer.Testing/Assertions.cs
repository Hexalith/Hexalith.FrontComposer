using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// High-signal DOM assertions for generated projection and DataGrid components.
/// </summary>
public static class GeneratedProjectionAssertions {
    /// <summary>Asserts that the rendered component contains the expected canonical headers in order.</summary>
    public static void AssertHeadersInOrder<TComponent>(IRenderedComponent<TComponent> rendered, params string[] headers)
        where TComponent : IComponent {
        ArgumentNullException.ThrowIfNull(rendered);
        ArgumentNullException.ThrowIfNull(headers);
        string markup = rendered.Markup;
        int previous = -1;
        foreach (string header in headers) {
            int current = markup.IndexOf($">{header}<", StringComparison.Ordinal);
            if (current < 0) {
                throw new InvalidOperationException($"Expected generated projection header '{header}' was not rendered.");
            }

            if (current <= previous) {
                throw new InvalidOperationException($"Expected generated projection header '{header}' to appear after the previous header.");
            }

            previous = current;
        }
    }

    /// <summary>Asserts that the rendered component includes every expected cell value.</summary>
    public static void AssertCellValues<TComponent>(IRenderedComponent<TComponent> rendered, params string[] values)
        where TComponent : IComponent {
        ArgumentNullException.ThrowIfNull(rendered);
        ArgumentNullException.ThrowIfNull(values);
        foreach (string value in values) {
            if (!rendered.Markup.Contains(value, StringComparison.Ordinal)) {
                throw new InvalidOperationException($"Expected generated projection cell value '{value}' was not rendered.");
            }
        }
    }

    /// <summary>Asserts that a generated DataGrid envelope is present.</summary>
    public static void AssertDataGridEnvelope<TComponent>(IRenderedComponent<TComponent> rendered, string? projectionName = null)
        where TComponent : IComponent {
        ArgumentNullException.ThrowIfNull(rendered);
        if (!rendered.Markup.Contains("data-fc-datagrid", StringComparison.Ordinal)) {
            throw new InvalidOperationException("Expected generated projection markup to include the FrontComposer DataGrid envelope.");
        }

        if (!string.IsNullOrWhiteSpace(projectionName)
            && !rendered.Markup.Contains(projectionName, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Expected generated DataGrid envelope to include projection marker '{projectionName}'.");
        }
    }

    /// <summary>Asserts that an element with the expected accessibility attribute exists.</summary>
    public static void AssertAccessibilityAttribute<TComponent>(
        IRenderedComponent<TComponent> rendered,
        string cssSelector,
        string attributeName,
        string expectedValue)
        where TComponent : IComponent {
        ArgumentNullException.ThrowIfNull(rendered);
        AngleSharp.Dom.IElement element = rendered.Find(cssSelector);
        string? actual = element.GetAttribute(attributeName);
        if (!string.Equals(actual, expectedValue, StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                $"Expected '{cssSelector}' to have {attributeName}='{expectedValue}', but found '{actual ?? "<missing>"}'.");
        }
    }
}

/// <summary>
/// Assertions for fake command evidence captured by <see cref="TestCommandService"/>.
/// </summary>
public static class CommandEvidenceAssertions {
    /// <summary>Asserts that command evidence contains a lifecycle state.</summary>
    public static void AssertLifecycleContains(CommandDispatchEvidence evidence, CommandLifecycleState expectedState) {
        ArgumentNullException.ThrowIfNull(evidence);
        if (!evidence.LifecycleStates.Contains(expectedState)) {
            throw new InvalidOperationException($"Expected command evidence to contain lifecycle state '{expectedState}'.");
        }
    }

    /// <summary>Asserts that redacted evidence does not expose raw tenant or user identifiers.</summary>
    public static void AssertRedacted(CommandDispatchEvidence evidence, string tenantId, string userId) {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.RedactedPayload.Contains(tenantId, StringComparison.Ordinal)
            || evidence.RedactedPayload.Contains(userId, StringComparison.Ordinal)) {
            throw new InvalidOperationException("Command evidence payload contains unredacted tenant or user identifiers.");
        }
    }
}
