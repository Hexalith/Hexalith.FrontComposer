using System;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Redacted numeric/boolean measurements for one canonical AE dimension; recording does not imply acceptance.</summary>
/// <param name="Row">Canonical evidence dimension.</param>
/// <param name="CapturedAtUtc">Configured-clock capture instant.</param>
/// <param name="Context">Safe ordinal surface/state and typed theme provenance.</param>
/// <param name="RedactedMeasurements">Bounded fixed-schema JSON of numbers, booleans, enums and geometry, without arbitrary text; explicitly truncated beyond the configured payload limit.</param>
public sealed record AccessibilityEvidence(AccessibilityEvidenceRow Row, DateTimeOffset CapturedAtUtc, AccessibilityEvidenceContext Context, string RedactedMeasurements);
