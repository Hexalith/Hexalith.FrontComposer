namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a class as a projection read model that the FrontComposer source generator processes
/// to emit Razor DataGrid UI, Fluxor state artifacts, MCP metadata, and domain registration code.
/// </summary>
/// <remarks>
/// For display metadata (Name, Description, Order, etc.), use
/// <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> from the BCL.
/// No custom display attribute is needed — the source generator reads DisplayAttribute
/// via Roslyn symbol analysis.
/// Generated projection files are written under
/// <see cref="Hexalith.FrontComposer.Contracts.Conformance.GeneratedOutputPathContract.Directory"/>
/// when compiler generated files are emitted. Diagnostics and IDE parity guidance are documented from
/// <see href="https://hexalith.github.io/FrontComposer/diagnostics/">the FrontComposer diagnostics pages</see>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ProjectionAttribute : Attribute {
}
