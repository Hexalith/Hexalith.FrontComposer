using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Contracts.Communication;

#pragma warning disable HFC0001, CS0618 // This converter is the bounded v1.12 compatibility boundary.

/// <summary>
/// Preserves the flattened v1.12 JSON contract without requiring serializers or source generators
/// to bind obsolete compatibility members.
/// </summary>
public sealed class QueryRequestJsonConverter : JsonConverter<QueryRequest>
{
    private static readonly string[] SerializedPropertyNames =
    [
        nameof(QueryRequest.ProjectionType),
        nameof(QueryRequest.TenantId),
        nameof(QueryRequest.Filter),
        nameof(QueryRequest.Skip),
        nameof(QueryRequest.Take),
        nameof(QueryRequest.ETag),
        nameof(QueryRequest.ColumnFilters),
        nameof(QueryRequest.StatusFilters),
        nameof(QueryRequest.SearchQuery),
        nameof(QueryRequest.SortColumn),
        nameof(QueryRequest.SortDescending),
        nameof(QueryRequest.Domain),
        nameof(QueryRequest.AggregateId),
        nameof(QueryRequest.QueryType),
        nameof(QueryRequest.EntityId),
        nameof(QueryRequest.ProjectionActorType),
        nameof(QueryRequest.ETags),
        nameof(QueryRequest.CacheDiscriminator),
        nameof(QueryRequest.CachePayloadVersion),
    ];

    /// <summary>Initializes a new converter instance.</summary>
    public QueryRequestJsonConverter()
    {
    }

    /// <inheritdoc />
    public override QueryRequest Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("QueryRequest JSON must be an object.");
        }

        ValidateNoUnmappedMembers(root, options);

        string projectionType = ReadString(root, nameof(QueryRequest.ProjectionType), options)!;
        string? tenantId = ReadString(root, nameof(QueryRequest.TenantId), options);
        string? filter = ReadString(root, nameof(QueryRequest.Filter), options);
        int? skip = ReadNullableInt32(root, nameof(QueryRequest.Skip), options);
        int? take = ReadNullableInt32(root, nameof(QueryRequest.Take), options);
        string? etag = ReadString(root, nameof(QueryRequest.ETag), options);
        IReadOnlyDictionary<string, string>? columnFilters = ReadDictionary(root, nameof(QueryRequest.ColumnFilters), options);
        IReadOnlyList<string>? statusFilters = ReadList(root, nameof(QueryRequest.StatusFilters), options);
        string? searchQuery = ReadString(root, nameof(QueryRequest.SearchQuery), options);
        string? sortColumn = ReadString(root, nameof(QueryRequest.SortColumn), options);
        bool sortDescending = ReadBoolean(root, nameof(QueryRequest.SortDescending), options);
        string? domain = ReadString(root, nameof(QueryRequest.Domain), options);
        string? aggregateId = ReadString(root, nameof(QueryRequest.AggregateId), options);
        string? queryType = ReadString(root, nameof(QueryRequest.QueryType), options);
        string? entityId = ReadString(root, nameof(QueryRequest.EntityId), options);
        string? projectionActorType = ReadString(root, nameof(QueryRequest.ProjectionActorType), options);
        IReadOnlyList<string>? etags = ReadList(root, nameof(QueryRequest.ETags), options);
        string? cacheDiscriminator = ReadString(root, nameof(QueryRequest.CacheDiscriminator), options);
        int cachePayloadVersion = ReadInt32(root, nameof(QueryRequest.CachePayloadVersion), defaultValue: 1, options);

        return new QueryRequest(
            projectionType,
            tenantId,
            filter,
            skip,
            take,
            etag,
            columnFilters,
            statusFilters,
            searchQuery,
            sortColumn,
            sortDescending,
            domain,
            aggregateId,
            queryType,
            entityId,
            projectionActorType,
            etags,
            cacheDiscriminator,
            cachePayloadVersion);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, QueryRequest value, JsonSerializerOptions options)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        writer.WriteStartObject();
        WriteString(writer, nameof(QueryRequest.ProjectionType), value.Criteria.ProjectionType, options);
        WriteString(writer, nameof(QueryRequest.TenantId), value.TenantId, options);
        WriteString(writer, nameof(QueryRequest.Filter), value.LegacyFilter, options);
        WriteNumber(writer, nameof(QueryRequest.Skip), value.Criteria.Skip, options);
        WriteNumber(writer, nameof(QueryRequest.Take), value.Criteria.Take, options);
        WriteString(writer, nameof(QueryRequest.ETag), value.ETag, options);
        WriteDictionary(writer, nameof(QueryRequest.ColumnFilters), value.Criteria.ColumnFilters, options);
        WriteList(writer, nameof(QueryRequest.StatusFilters), value.Criteria.StatusFilters, options);
        WriteString(writer, nameof(QueryRequest.SearchQuery), value.Criteria.SearchQuery, options);
        WriteString(writer, nameof(QueryRequest.SortColumn), value.Criteria.SortColumn, options);
        if (value.Criteria.SortDescending || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingDefault)
        {
            writer.WriteBoolean(PropertyName(nameof(QueryRequest.SortDescending), options), value.Criteria.SortDescending);
        }
        WriteString(writer, nameof(QueryRequest.Domain), value.Domain, options);
        WriteString(writer, nameof(QueryRequest.AggregateId), value.AggregateId, options);
        WriteString(writer, nameof(QueryRequest.QueryType), value.QueryType, options);
        WriteString(writer, nameof(QueryRequest.EntityId), value.EntityId, options);
        WriteString(writer, nameof(QueryRequest.ProjectionActorType), value.ProjectionActorType, options);
        WriteList(writer, nameof(QueryRequest.ETags), value.ETags, options);
        WriteString(writer, nameof(QueryRequest.CacheDiscriminator), value.CacheDiscriminator, options);
        if (value.CachePayloadVersion != 0 || options.DefaultIgnoreCondition != JsonIgnoreCondition.WhenWritingDefault)
        {
            WriteNumber(writer, nameof(QueryRequest.CachePayloadVersion), value.CachePayloadVersion, options);
        }
        writer.WriteEndObject();
    }

    private static bool TryGetProperty(JsonElement root, string clrName, JsonSerializerOptions options, out JsonElement value)
    {
        string propertyName = PropertyName(clrName, options);
        if (root.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (!options.PropertyNameCaseInsensitive)
        {
            return false;
        }

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

    private static void ValidateNoUnmappedMembers(JsonElement root, JsonSerializerOptions options)
    {
        if (options.UnmappedMemberHandling != JsonUnmappedMemberHandling.Disallow)
        {
            return;
        }

        StringComparison comparison = options.PropertyNameCaseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            bool known = SerializedPropertyNames.Any(
                clrName => string.Equals(property.Name, PropertyName(clrName, options), comparison));
            if (!known)
            {
                throw new JsonException($"The JSON property '{property.Name}' could not be mapped to QueryRequest.");
            }
        }
    }

    private static string PropertyName(string clrName, JsonSerializerOptions options)
        => options.PropertyNamingPolicy?.ConvertName(clrName) ?? clrName;

    private static string? ReadString(JsonElement root, string clrName, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, clrName, options, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"QueryRequest property '{PropertyName(clrName, options)}' must be a string or null.");
        }

        return value.GetString();
    }

    private static int? ReadNullableInt32(JsonElement root, string clrName, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, clrName, options, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return ParseInt32(value, clrName, options);
    }

    private static int ReadInt32(JsonElement root, string clrName, int defaultValue, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, clrName, options, out JsonElement value))
        {
            return defaultValue;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException($"QueryRequest property '{PropertyName(clrName, options)}' cannot be null.");
        }

        return ParseInt32(value, clrName, options);
    }

    private static int ParseInt32(JsonElement value, string clrName, JsonSerializerOptions options)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && (options.NumberHandling & JsonNumberHandling.AllowReadingFromString) != 0
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int quotedNumber))
        {
            return quotedNumber;
        }

        throw new JsonException($"QueryRequest property '{PropertyName(clrName, options)}' must be a 32-bit integer.");
    }

    private static bool ReadBoolean(JsonElement root, string clrName, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, clrName, options, out JsonElement value))
        {
            return false;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        throw new JsonException($"QueryRequest property '{PropertyName(clrName, options)}' must be a boolean.");
    }

    private static IReadOnlyDictionary<string, string>? ReadDictionary(JsonElement root, string clrName, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, clrName, options, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"QueryRequest property '{PropertyName(clrName, options)}' must be an object or null.");
        }

        Dictionary<string, string> result = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
            {
                throw new JsonException($"QueryRequest dictionary value '{property.Name}' must be a string or null.");
            }

            result[property.Name] = property.Value.GetString()!;
        }

        return result;
    }

    private static IReadOnlyList<string>? ReadList(JsonElement root, string clrName, JsonSerializerOptions options)
    {
        if (!TryGetProperty(root, clrName, options, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"QueryRequest property '{PropertyName(clrName, options)}' must be an array or null.");
        }

        List<string> result = [];
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
            {
                throw new JsonException($"QueryRequest array '{PropertyName(clrName, options)}' must contain only strings or nulls.");
            }

            result.Add(item.GetString()!);
        }

        return result;
    }

    private static void WriteNumber(Utf8JsonWriter writer, string clrName, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            WriteNumber(writer, clrName, value.Value, options);
        }
        else
        {
            WriteNull(writer, clrName, options);
        }
    }

    private static void WriteNumber(Utf8JsonWriter writer, string clrName, int value, JsonSerializerOptions options)
    {
        string propertyName = PropertyName(clrName, options);
        if ((options.NumberHandling & JsonNumberHandling.WriteAsString) != 0)
        {
            writer.WriteString(propertyName, value.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNumber(propertyName, value);
        }
    }

    private static void WriteString(Utf8JsonWriter writer, string clrName, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            WriteNull(writer, clrName, options);
        }
        else
        {
            writer.WriteString(PropertyName(clrName, options), value);
        }
    }

    private static void WriteDictionary(
        Utf8JsonWriter writer,
        string clrName,
        IReadOnlyDictionary<string, string>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            if (!ShouldOmitNull(options))
            {
                writer.WritePropertyName(PropertyName(clrName, options));
                writer.WriteNullValue();
            }

            return;
        }

        writer.WritePropertyName(PropertyName(clrName, options));
        writer.WriteStartObject();
        foreach (KeyValuePair<string, string> item in value)
        {
            string key = options.DictionaryKeyPolicy?.ConvertName(item.Key) ?? item.Key;
            writer.WriteString(key, item.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteList(
        Utf8JsonWriter writer,
        string clrName,
        IReadOnlyList<string>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            if (!ShouldOmitNull(options))
            {
                writer.WritePropertyName(PropertyName(clrName, options));
                writer.WriteNullValue();
            }

            return;
        }

        writer.WritePropertyName(PropertyName(clrName, options));
        writer.WriteStartArray();
        foreach (string item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }

    private static void WriteNull(Utf8JsonWriter writer, string clrName, JsonSerializerOptions options)
    {
        if (!ShouldOmitNull(options))
        {
            writer.WriteNull(PropertyName(clrName, options));
        }
    }

    private static bool ShouldOmitNull(JsonSerializerOptions options)
        => options.DefaultIgnoreCondition is JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault;
}

#pragma warning restore HFC0001, CS0618
