
using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public class FieldTypeMapperTests {
    [Theory]
    [InlineData("System.String", "String")]
    [InlineData("string", "String")]
    [InlineData("System.Int32", "Int32")]
    [InlineData("int", "Int32")]
    [InlineData("System.Int64", "Int64")]
    [InlineData("long", "Int64")]
    [InlineData("System.Decimal", "Decimal")]
    [InlineData("decimal", "Decimal")]
    [InlineData("System.Double", "Double")]
    [InlineData("double", "Double")]
    [InlineData("System.Single", "Single")]
    [InlineData("float", "Single")]
    [InlineData("System.Boolean", "Boolean")]
    [InlineData("bool", "Boolean")]
    public void MapType_Primitives_ReturnsCorrectIrType(string dotNetType, string expectedIrType) {
        string? result = FieldTypeMapper.MapType(dotNetType, isEnum: false);

        result.ShouldBe(expectedIrType);
    }

    [Theory]
    [InlineData("System.DateTime", "DateTime")]
    [InlineData("System.DateTimeOffset", "DateTimeOffset")]
    [InlineData("System.DateOnly", "DateOnly")]
    [InlineData("System.TimeOnly", "TimeOnly")]
    public void MapType_DateTimeTypes_ReturnsCorrectIrType(string dotNetType, string expectedIrType) {
        string? result = FieldTypeMapper.MapType(dotNetType, isEnum: false);

        result.ShouldBe(expectedIrType);
    }

    [Fact]
    public void MapType_Guid_ReturnsGuid() {
        string? result = FieldTypeMapper.MapType("System.Guid", isEnum: false);

        result.ShouldBe("Guid");
    }

    [Fact]
    public void MapType_Enum_ReturnsEnum() {
        string? result = FieldTypeMapper.MapType("TestDomain.SomeStatus", isEnum: true);

        result.ShouldBe("Enum");
    }

    [Theory]
    [InlineData("System.Collections.Generic.List")]
    [InlineData("System.Collections.Generic.IEnumerable")]
    [InlineData("System.Collections.Generic.IReadOnlyList")]
    public void MapType_CollectionTypes_ReturnsCollection(string collectionType) {
        string? result = FieldTypeMapper.MapType(collectionType + "<System.String>", isEnum: false);

        result.ShouldBe("Collection");
    }

    [Theory]
    [InlineData("System.Byte[]")]
    [InlineData("System.Collections.Generic.Dictionary")]
    [InlineData("System.Object")]
    [InlineData("SomeUnknown.CustomType")]
    public void MapType_UnsupportedTypes_ReturnsNull(string typeName) {
        string? result = FieldTypeMapper.MapType(typeName, isEnum: false);

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("System.Collections.Generic.List<System.String>")]
    [InlineData("System.Collections.Generic.IEnumerable<System.Int32>")]
    [InlineData("System.Collections.Generic.IReadOnlyList<System.Decimal>")]
    public void IsCollectionType_KnownCollections_ReturnsTrue(string typeName) {
        bool result = FieldTypeMapper.IsCollectionType(typeName);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("System.Collections.Generic.Dictionary<System.String, System.Int32>")]
    public void IsCollectionType_NonCollections_ReturnsFalse(string typeName) {
        bool result = FieldTypeMapper.IsCollectionType(typeName);

        result.ShouldBeFalse();
    }
}
