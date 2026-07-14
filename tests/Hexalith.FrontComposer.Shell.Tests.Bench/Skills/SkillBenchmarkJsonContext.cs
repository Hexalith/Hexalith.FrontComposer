using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Mcp.Skills;

[JsonSerializable(typeof(SkillBenchmarkPromptSetDto))]
[JsonSerializable(typeof(SkillBenchmarkResult))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class SkillBenchmarkJsonContext : JsonSerializerContext;
