namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Marks a class as a command that the FrontComposer source generator processes
/// to emit form components, renderer/page artifacts, Fluxor actions, lifecycle bridge code,
/// MCP metadata, and dispatch registration code.
/// </summary>
/// <remarks>
/// Generated command files are written under
/// <see cref="Hexalith.FrontComposer.Contracts.Conformance.GeneratedOutputPathContract.Directory"/>
/// when compiler generated files are emitted. Use the <c>frontcomposer inspect</c> command or IDE
/// generated-source navigation to inspect the generated contract. Diagnostics and IDE parity guidance are documented from
/// <see href="https://hexalith.github.io/FrontComposer/diagnostics/">the FrontComposer diagnostics pages</see>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute {
}
