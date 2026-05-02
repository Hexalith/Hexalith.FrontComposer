using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests;

public sealed class BoundaryTests {
    [Fact]
    public void ContractsAssembly_DoesNotReference_McpSdk() {
        typeof(Contracts.Mcp.McpManifest).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ShouldNotContain("ModelContextProtocol");
    }

    [Fact]
    public void ContractsAssembly_DoesNotReference_TransportPackages() {
        // Contracts must remain transport-agnostic so it can ship with non-HTTP hosts (workers,
        // gateways, in-process tests). Asserting these specific package names protects against
        // accidental coupling that would force every adopter to take an AspNetCore dependency.
        string[] referenced = [.. typeof(Contracts.Mcp.McpManifest).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)];

        referenced.ShouldNotContain("Microsoft.AspNetCore.Http");
        referenced.ShouldNotContain("Microsoft.AspNetCore.Http.Abstractions");
        referenced.ShouldNotContain("Microsoft.AspNetCore.Routing");
    }

    [Fact]
    public void SourceToolsAssembly_DoesNotReference_McpSdk() {
        typeof(SourceTools.FrontComposerGenerator).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ShouldNotContain("ModelContextProtocol");
    }

    [Fact]
    public void SourceToolsAssembly_DoesNotReference_McpRuntimePackage() {
        // Ensures Source generators stay free of the MCP runtime-host package.
        typeof(SourceTools.FrontComposerGenerator).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ShouldNotContain("Hexalith.FrontComposer.Mcp");
    }
}
