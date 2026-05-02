using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

internal static class McpJsonSchemaBuilder {
    public static JsonElement BuildInputSchema(IReadOnlyList<McpParameterDescriptor> parameters) {
        JsonObject properties = [];
        JsonArray required = [];
        foreach (McpParameterDescriptor parameter in parameters
            .Where(p => !p.IsUnsupported)
            .OrderBy(p => p.Name, StringComparer.Ordinal)) {
            JsonObject property = new() {
                ["type"] = parameter.JsonType,
                ["title"] = parameter.Title,
            };
            if (!string.IsNullOrWhiteSpace(parameter.Description)) {
                property["description"] = parameter.Description;
            }

            if (parameter.EnumValues.Count > 0) {
                JsonArray values = [];
                foreach (string value in parameter.EnumValues) {
                    values.Add(value);
                }

                property["enum"] = values;
            }

            properties[parameter.Name] = property;
            if (parameter.IsRequired) {
                required.Add(parameter.Name);
            }
        }

        JsonObject schema = new() {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = properties,
        };
        if (required.Count > 0) {
            schema["required"] = required;
        }

        return schema.Deserialize<JsonElement>();
    }
}
