namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Records a failed eligible background read without publishing a new operator outcome.</summary>
/// <param name="ViewKey">The originating view.</param>
/// <param name="Skip">The failed page offset.</param>
/// <param name="OriginScopeGeneration">The scope captured before the read.</param>
/// <param name="RequestIdentity">The originating criteria.</param>
/// <param name="RequestGeneration">The originating criteria generation.</param>
public sealed record MarkProjectionStaleAction(string ViewKey, int Skip, long OriginScopeGeneration, string RequestIdentity, long? RequestGeneration);
