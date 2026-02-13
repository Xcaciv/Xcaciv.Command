using Xunit;
using Xcaciv.Command.Core.Parameters;
using Xcaciv.Command.Interface.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Xcaciv.Command.Tests.Parameters;

public class DefaultParameterConverterTests
{
    private readonly DefaultParameterConverter _converter = new();

    [Fact]
    public void CanConvert_WithString_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(string)));
    }

    [Fact]
    public void CanConvert_WithInt_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(int)));
    }

    [Fact]
    public void CanConvert_WithFloat_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(float)));
    }

    [Fact]
    public void CanConvert_WithGuid_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(Guid)));
    }

    [Fact]
    public void CanConvert_WithJsonElement_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(JsonElement)));
    }

    [Fact]
    public void CanConvert_WithUnsupportedType_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(object)));
    }

    [Fact]
    public void Convert_StringValue_ReturnsString()
    {
        var result = _converter.Convert("hello", typeof(string));

        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorMessage}");
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Convert_ValidInt_ReturnsInt()
    {
        var result = _converter.Convert("42", typeof(int));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Convert_InvalidInt_ReturnsError()
    {
        var result = _converter.Convert("not-a-number", typeof(int));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Convert_ValidFloat_ReturnsFloat()
    {
        var result = _converter.Convert("3.14", typeof(float));

        Assert.True(result.IsSuccess);
        Assert.Equal(3.14f, (float)result.Value!, 2);
    }

    [Fact]
    public void Convert_ValidBooleanTrue_ReturnsTrue()
    {
        var result = _converter.Convert("true", typeof(bool));

        Assert.True(result.IsSuccess);
        Assert.True((bool)result.Value!);
    }

    [Fact]
    public void Convert_BooleanOne_ReturnsTrue()
    {
        var result = _converter.Convert("1", typeof(bool));

        Assert.True(result.IsSuccess);
        Assert.True((bool)result.Value!);
    }

    [Fact]
    public void Convert_BooleanYes_ReturnsTrue()
    {
        var result = _converter.Convert("yes", typeof(bool));

        Assert.True(result.IsSuccess);
        Assert.True((bool)result.Value!);
    }

    [Fact]
    public void Convert_BooleanFalse_ReturnsFalse()
    {
        var result = _converter.Convert("false", typeof(bool));

        Assert.True(result.IsSuccess);
        Assert.False((bool)result.Value!);
    }

    [Fact]
    public void Convert_ValidGuid_ReturnsGuid()
    {
        var guid = "550e8400-e29b-41d4-a716-446655440000";
        var result = _converter.Convert(guid, typeof(Guid));

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Parse(guid), result.Value);
    }

    [Fact]
    public void Convert_InvalidGuid_ReturnsError()
    {
        var result = _converter.Convert("not-a-guid", typeof(Guid));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Convert_ValidJsonObject_ReturnsJsonElement()
    {
        var json = @"{""name"":""test"",""value"":42}";
        var result = _converter.Convert(json, typeof(JsonElement));

        Assert.True(result.IsSuccess);
        Assert.IsType<JsonElement>(result.Value);
    }

    [Fact]
    public void Convert_InvalidJson_ReturnsError()
    {
        var result = _converter.Convert("{invalid json}", typeof(JsonElement));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Convert_EmptyStringToInt_ReturnsError()
    {
        var result = _converter.Convert("", typeof(int));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }
}

public class ParameterValueTests
{
    private readonly DefaultParameterConverter _converter = new();

    [Fact]
    public void Constructor_WithValidData_InitializesProperties()
    {
        var paramValue = CreateParameterValue("name", "42", typeof(int));

        Assert.Equal("name", paramValue.Name);
        Assert.Equal(typeof(int), paramValue.DataType);
        // RawValue is no longer public - it's stored internally for error messages only
        Assert.Equal(42, paramValue.GetValue<int>()); // Verify converted value instead
    }

    [Fact]
    public void Value_WithValidConversion_ReturnsConvertedValue()
    {
        var paramValue = CreateParameterValue("count", "42", typeof(int));

        Assert.True(paramValue.IsValid);
        Assert.Equal(42, paramValue.GetValue<int>());
    }

    [Fact]
    public void Value_WithInvalidConversion_SetsValidationError()
    {
        var paramValue = CreateParameterValue("count", "not-a-number", typeof(int));

        Assert.False(paramValue.IsValid);
        Assert.NotNull(paramValue.ValidationError);
    }

    [Fact]
    public void As_WithCorrectType_ReturnsValue()
    {
        var paramValue = CreateParameterValue("text", "hello", typeof(string));

        var result = paramValue.GetValue<string>();
        Assert.Equal("hello", result);
    }

    [Fact]
    public void As_WithValueType_ReturnsValue()
    {
        var paramValue = CreateParameterValue("count", "42", typeof(int));

        var result = paramValue.GetValue<int>();
        Assert.Equal(42, result);
    }

    private IParameterValue CreateParameterValue(string name, string rawValue, Type targetType)
    {
        var factory = new ParameterValueFactory();
        var convertedValue = _converter.ValidateAndConvert(name, rawValue, targetType, out var validationError, out var isValid);
        return factory.Create(name, rawValue, convertedValue, targetType, isValid, validationError);
    }
}

public class ParameterCollectionTests
{
    private readonly DefaultParameterConverter _converter = new();

    [Fact]
    public void Constructor_CreatesDictionaryWithCaseInsensitivity()
    {
        var collection = new ParameterCollection();

        var param = CreateParameterValue("Name", "test", typeof(string));
        collection["name"] = param;

        // Should find by different case
        Assert.NotNull(collection.GetParameter("NAME"));
        Assert.NotNull(collection.GetParameter("Name"));
    }

    [Fact]
    public void GetParameter_WithExistingParameter_ReturnsIt()
    {
        var param = CreateParameterValue("name", "test", typeof(string));
        var collection = new ParameterCollection { { "name", param } };

        var result = collection.GetParameter("name");
        Assert.NotNull(result);
        Assert.Equal("test", result.GetValue<string>());
    }

    [Fact]
    public void GetParameter_WithNonExistent_ReturnsNull()
    {
        var collection = new ParameterCollection();

        var result = collection.GetParameter("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void GetParameterRequired_WithNonExistent_ThrowsException()
    {
        var collection = new ParameterCollection();

        Assert.Throws<KeyNotFoundException>(() => collection.GetParameterRequired("nonexistent"));
    }

    [Fact]
    public void GetAsValueType_WithValidParameter_ReturnsTypedValue()
    {
        var param = CreateParameterValue("count", "42", typeof(int));
        var collection = new ParameterCollection { { "count", param } };

        var result = collection.GetAsValueType<int>("count");
        Assert.Equal(42, result);
    }

    [Fact]
    public void AreAllValid_WithValidParameters_ReturnsTrue()
    {
        var param = CreateParameterValue("count", "42", typeof(int));
        var collection = new ParameterCollection { { "count", param } };

        Assert.True(collection.AreAllValid());
    }

    [Fact]
    public void AreAllValid_WithInvalidParameter_ReturnsFalse()
    {
        var param = CreateParameterValue("count", "invalid", typeof(int));
        var collection = new ParameterCollection { { "count", param } };

        Assert.False(collection.AreAllValid());
    }

    private IParameterValue CreateParameterValue(string name, string rawValue, Type targetType)
    {
        var factory = new ParameterValueFactory();
        var convertedValue = _converter.ValidateAndConvert(name, rawValue, targetType, out var validationError, out var isValid);
        return factory.Create(name, rawValue, convertedValue, targetType, isValid, validationError);
    }
}

public class ParameterCollectionBuilderTests
{
    private readonly DefaultParameterConverter _converter = new();
    private readonly ParameterCollectionBuilder _builder;

    public ParameterCollectionBuilderTests()
    {
        _builder = new ParameterCollectionBuilder(_converter);
    }

    [Fact]
    public void Build_WithValidParameters_CreatesCollection()
    {
        var dict = new Dictionary<string, string>
        {
            { "name", "test" },
            { "count", "42" }
        };
        var attrs = new[]
        {
            new TestParameterAttribute("name", typeof(string)),
            new TestParameterAttribute("count", typeof(int))
        };

        var collection = _builder.Build(dict, attrs);

        Assert.Equal(2, collection.Count);
        Assert.Equal("test", collection.GetParameter("name")?.GetValue<string>());
        Assert.Equal(42, collection.GetAsValueType<int>("count"));
    }

    [Fact]
    public void Build_WithInvalidParameter_ThrowsAggregateException()
    {
        var dict = new Dictionary<string, string>
        {
            { "count", "invalid" }
        };
        var attrs = new[] { new TestParameterAttribute("count", typeof(int)) };

        var ex = Assert.Throws<ArgumentException>(() => _builder.Build(dict, attrs));
        Assert.Contains("Parameter validation failed", ex.Message);
    }

    [Fact]
    public void BuildStrict_WithValidParameters_CreatesCollection()
    {
        var dict = new Dictionary<string, string> { { "count", "42" } };
        var attr = new[] { new TestParameterAttribute("count", typeof(int)) };

        var collection = _builder.BuildStrict(dict, attr);

        Assert.Equal(1, collection.Count);
        Assert.Equal(42, collection.GetAsValueType<int>("count"));
    }

    [Fact]
    public void BuildStrict_WithInvalidParameter_ThrowsException()
    {
        var dict = new Dictionary<string, string> { { "count", "invalid" } };
        var attr = new[] { new TestParameterAttribute("count", typeof(int)) };

        Assert.Throws<ArgumentException>(() => _builder.BuildStrict(dict, attr));
    }
}

// Test helper for parameter attributes
public class TestParameterAttribute : Xcaciv.Command.Interface.Attributes.AbstractCommandParameterAttribute
{
    public TestParameterAttribute(string name, Type dataType)
    {
        Name = name;
        DataType = dataType;
    }
}

public class ParameterValueTypeConsistencyTests
{
    private readonly DefaultParameterConverter _converter = new();

    [Fact]
    public void Constructor_WithInvalidConversion_StoresSentinelNotString()
    {
        var param = CreateParameterValue("count", "invalid", typeof(int));

        Assert.False(param.IsValid);
        Assert.NotNull(param.ValidationError);
        
        // RawValue should always be the original string input
        Assert.IsType<string>(param.RawValue);
        Assert.Equal("invalid", param.RawValue);
        
        // UntypedValue should be the sentinel for invalid conversions
        var abstractParam = param as IParameterValue;
        Assert.NotNull(abstractParam);
        
        // Access UntypedValue through reflection since it's not in the interface
        var untypedValueProp = param.GetType().GetProperty("UntypedValue");
        Assert.NotNull(untypedValueProp);
        var untypedValue = untypedValueProp.GetValue(param);
        Assert.IsType<InvalidParameterValue>(untypedValue);
    }

    [Fact]
    public void As_WithInvalidParameter_ThrowsWithDetailedMessage()
    {
        var param = CreateParameterValue("count", "invalid", typeof(int));

        var ex = Assert.Throws<InvalidOperationException>(() => param.GetValue<int>());
        
        Assert.Contains("validation error", ex.Message);
        Assert.Contains("count", ex.Message);
        Assert.Contains("invalid", ex.Message);  // Raw value in error
    }

    [Fact]
    public void As_WithWrongTypeRequest_ThrowsWithDiagnostics()
    {
        var param = CreateParameterValue("count", "42", typeof(int));

        var ex = Assert.Throws<InvalidCastException>(() => param.GetValue<string>());
        
        Assert.Contains("Type mismatch", ex.Message);
        Assert.Contains("Stored as: Int32", ex.Message);
        Assert.Contains("Requested as: String", ex.Message);
    }

    [Fact]
    public void Constructor_WithUnsupportedType_Throws()
    {
        Assert.Throws<ArgumentException>(() => 
            CreateParameterValue("obj", "test", typeof(object)));
    }

    [Fact]
    public void GetValue_WithValidParameter_ReturnsCorrectType()
    {
        var collection = new ParameterCollection();
        collection["count"] = CreateParameterValue("count", "42", typeof(int));

        int count = collection.GetValue<int>("count");
        
        Assert.Equal(42, count);
    }

    [Fact]
    public void GetValue_WithInvalidParameter_ThrowsInvalidOperation()
    {
        var collection = new ParameterCollection();
        collection["count"] = CreateParameterValue("count", "invalid", typeof(int));

        var ex = Assert.Throws<InvalidOperationException>(() => 
            collection.GetValue<int>("count"));
        
        Assert.Contains("validation error", ex.Message);
    }

    private IParameterValue CreateParameterValue(string name, string rawValue, Type targetType)
    {
        var factory = new ParameterValueFactory();
        var convertedValue = _converter.ValidateAndConvert(name, rawValue, targetType, out var validationError, out var isValid);
        return factory.Create(name, rawValue, convertedValue, targetType, isValid, validationError);
    }
}
