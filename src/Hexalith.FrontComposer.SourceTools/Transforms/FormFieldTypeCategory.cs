namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Categorizes a command property type for form field rendering.
/// </summary>
/// <remarks>
/// Command forms render a richer set of input controls than projection grids, so this
/// enumeration is more granular than <see cref="TypeCategory"/>. The mapping drives the
/// per-field emission table in the command form emitter.
/// </remarks>
public enum FormFieldTypeCategory {
    /// <summary><c>FluentTextInput</c> with the default string binding.</summary>
    TextInput,

    /// <summary><c>FluentTextInput</c> with <c>TextInputType.Number</c> and an integer string-backing converter.</summary>
    NumberInput,

    /// <summary><c>FluentTextInput</c> with <c>TextInputType.Number</c>, <c>InputMode.Decimal</c>, and a decimal string-backing converter.</summary>
    DecimalInput,

    /// <summary><c>FluentSwitch</c> for a binary on/off toggle.</summary>
    Switch,

    /// <summary><c>FluentDatePicker&lt;TValue&gt;</c>.</summary>
    DatePicker,

    /// <summary><c>FluentSelect&lt;TEnum, TEnum&gt;</c> populated with enum members (humanized labels truncated to 30 chars).</summary>
    Select,

    /// <summary><c>FluentTextInput</c> styled with a monospace CSS class for identifier values (Guid, ULID).</summary>
    MonospaceText,

    /// <summary>
    /// <c>FluentTextInput</c> with <c>TextInputType.Time</c> and an <c>HH:mm</c> placeholder,
    /// mapped from <see cref="System.TimeOnly"/>. Task 3B.1. Patch 2026-04-16 P-06.
    /// </summary>
    TimeInput,

    /// <summary><c>FcFieldPlaceholder</c> indicating the field requires a custom renderer.</summary>
    Placeholder,
}
