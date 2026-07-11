using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Contracts.Tests.Communication;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(QueryRequest))]
internal sealed partial class QueryRequestJsonContext : JsonSerializerContext;
