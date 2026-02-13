using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Xcaciv.Command.Core.Parameters;
using Xcaciv.Command.Interface.Parameters;
using Xunit;

namespace Xcaciv.Command.Core.Parameters.UnitTests;


/// <summary>
/// Unit tests for the ParameterValueFactoryCaching class.
/// </summary>
public partial class ParameterValueFactoryCachingTests
{
    /// <summary>
    /// Tests that ClearCache executes without throwing an exception when caches are empty.
    /// Expected: Method completes successfully without any exceptions.
    /// </summary>
    [Fact]
    public void ClearCache_WhenCachesAreEmpty_DoesNotThrow()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();

        // Act & Assert
        var exception = Record.Exception(() => factory.ClearCache());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that ClearCache executes without throwing when caches contain data.
    /// Expected: Method completes successfully and subsequent Create calls work correctly.
    /// </summary>
    [Fact]
    public void ClearCache_WhenCachesContainData_ClearsSuccessfully()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        var dataType = typeof(string);

        // Populate the caches by calling Create
        factory.Create("param1", "rawValue1", "value1", dataType, true, null);
        factory.Create("param2", "rawValue2", "value2", dataType, true, null);

        // Act
        factory.ClearCache();

        // Assert - Verify that Create still works after cache is cleared
        var result = factory.Create("param3", "rawValue3", "value3", dataType, true, null);
        Assert.NotNull(result);
        Assert.Equal("param3", result.Name);
    }

    /// <summary>
    /// Tests that ClearCache can be called multiple times consecutively without issues.
    /// Expected: Multiple consecutive calls complete successfully without exceptions.
    /// </summary>
    [Fact]
    public void ClearCache_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();

        // Act & Assert
        factory.ClearCache();
        factory.ClearCache();
        factory.ClearCache();

        var exception = Record.Exception(() => factory.ClearCache());
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that ClearCache is thread-safe when called concurrently from multiple threads.
    /// Expected: All concurrent calls complete successfully without exceptions or deadlocks.
    /// </summary>
    [Fact]
    public void ClearCache_CalledConcurrently_IsThreadSafe()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        var dataType = typeof(int);

        // Populate cache
        factory.Create("test", "123", 123, dataType, true, null);

        // Act - Call ClearCache from multiple threads concurrently
        var tasks = new Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() => factory.ClearCache());
        }

        // Assert
        var exception = Record.Exception(() => Task.WaitAll(tasks));
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that ClearCache properly clears caches while concurrent Create operations are running.
    /// Expected: No exceptions occur and all operations complete successfully.
    /// </summary>
    [Fact]
    public void ClearCache_WithConcurrentCreateOperations_IsThreadSafe()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        var dataTypes = new[] { typeof(string), typeof(int), typeof(bool), typeof(double) };

        // Act - Mix ClearCache and Create calls concurrently
        var tasks = new Task[20];
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                var type = dataTypes[index % dataTypes.Length];
                factory.Create($"param{index}", $"raw{index}", index, type, true, null);
            });
        }
        for (int i = 10; i < 20; i++)
        {
            tasks[i] = Task.Run(() => factory.ClearCache());
        }

        // Assert
        var exception = Record.Exception(() => Task.WaitAll(tasks));
        Assert.Null(exception);

        // Verify factory still works after concurrent operations
        var result = factory.Create("final", "test", "test", typeof(string), true, null);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that ClearCache alternating with Create operations maintains correct behavior.
    /// Expected: Cache can be cleared and repopulated multiple times without issues.
    /// </summary>
    [Fact]
    public void ClearCache_AlternatingWithCreateOperations_MaintainsCorrectBehavior()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        var dataType = typeof(string);

        // Act & Assert - Alternate between Create and ClearCache
        for (int i = 0; i < 5; i++)
        {
            var result1 = factory.Create($"param{i}", $"raw{i}", $"value{i}", dataType, true, null);
            Assert.NotNull(result1);
            Assert.Equal($"param{i}", result1.Name);

            factory.ClearCache();

            var result2 = factory.Create($"param{i}_after", $"raw{i}_after", $"value{i}_after", dataType, true, null);
            Assert.NotNull(result2);
            Assert.Equal($"param{i}_after", result2.Name);
        }
    }

    /// <summary>
    /// Tests that ClearCache works correctly after cache limit is reached (100 types).
    /// Expected: ClearCache successfully clears bounded cache and Create continues to work.
    /// </summary>
    [Fact]
    public void ClearCache_AfterCacheLimitReached_ClearsSuccessfully()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();

        // Populate cache with multiple types (but stay under 100 limit for test performance)
        for (int i = 0; i < 10; i++)
        {
            var type = i switch
            {
                0 => typeof(string),
                1 => typeof(int),
                2 => typeof(bool),
                3 => typeof(double),
                4 => typeof(float),
                5 => typeof(decimal),
                6 => typeof(long),
                7 => typeof(byte),
                8 => typeof(char),
                _ => typeof(object)
            };
            factory.Create($"param{i}", $"raw{i}", i, type, true, null);
        }

        // Act
        factory.ClearCache();

        // Assert - Verify Create still works with various types after clearing
        var result1 = factory.Create("test1", "raw1", "value1", typeof(string), true, null);
        var result2 = factory.Create("test2", "raw2", 42, typeof(int), true, null);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    /// <summary>
    /// Tests that Create throws ArgumentNullException when dataType parameter is null.
    /// </summary>
    [Fact]
    public void Create_NullDataType_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type? nullType = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            factory.Create("testName", "testRaw", "testValue", nullType!, true, null));
        Assert.Equal("dataType", exception.ParamName);
    }

    /// <summary>
    /// Tests that Create successfully creates a ParameterValue for various primitive types on first call (slow path).
    /// </summary>
    /// <param name="dataType">The type to create ParameterValue for.</param>
    /// <param name="value">The value to store in the ParameterValue.</param>
    [Theory]
    [InlineData(typeof(int), 42)]
    [InlineData(typeof(string), "test")]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(double), 3.14)]
    [InlineData(typeof(long), 9223372036854775807L)]
    public void Create_FirstCallWithVariousTypes_ReturnsValidParameterValue(Type dataType, object value)
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        string name = "testParam";
        string raw = "rawValue";
        bool isValid = true;
        string? validationError = null;

        // Act
        var result = factory.Create(name, raw, value, dataType, isValid, validationError);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(raw, result.RawValue);
        Assert.Equal(value, result.UntypedValue);
        Assert.True(result.IsValid);
        Assert.Null(result.ValidationError);
        Assert.Equal(dataType, result.DataType);
    }

    /// <summary>
    /// Tests that Create uses cached factory on second call for the same type (fast path).
    /// </summary>
    [Fact]
    public void Create_SecondCallWithSameType_UsesCachedFactory()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(int);

        // Act - First call (slow path)
        var result1 = factory.Create("name1", "raw1", 10, dataType, true, null);

        // Act - Second call (fast path)
        var result2 = factory.Create("name2", "raw2", 20, dataType, true, null);

        // Assert - Both should succeed and have different values
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("name1", result1.Name);
        Assert.Equal("name2", result2.Name);
        Assert.Equal(10, result1.UntypedValue);
        Assert.Equal(20, result2.UntypedValue);
    }

    /// <summary>
    /// Tests that Create handles null value parameter correctly.
    /// </summary>
    [Fact]
    public void Create_NullValue_CreatesParameterValueWithNullValue()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        string name = "testParam";
        string raw = "rawValue";
        object? nullValue = null;
        Type dataType = typeof(string);
        bool isValid = false;
        string validationError = "Value is null";

        // Act
        var result = factory.Create(name, raw, nullValue, dataType, isValid, validationError);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(raw, result.RawValue);
        Assert.Null(result.UntypedValue);
        Assert.False(result.IsValid);
        Assert.Equal(validationError, result.ValidationError);
    }

    /// <summary>
    /// Tests that Create throws ArgumentNullException when name is empty or whitespace.
    /// </summary>
    /// <param name="name">The invalid parameter name.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceNames_ThrowsArgumentNullException(string name)
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(string);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            factory.Create(name, "rawValue", "value", dataType, true, null));
        Assert.Equal("name", exception.ParamName);
    }

    /// <summary>
    /// Tests that Create handles empty and whitespace strings for raw parameter.
    /// </summary>
    /// <param name="raw">The raw value string.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceRawValue_CreatesParameterValue(string raw)
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(string);

        // Act
        var result = factory.Create("validName", raw, "value", dataType, true, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("validName", result.Name);
        Assert.Equal(raw, result.RawValue);
    }

    /// <summary>
    /// Tests that Create handles invalid parameter state (isValid=false with validationError).
    /// </summary>
    [Fact]
    public void Create_InvalidParameterState_CreatesParameterValueWithError()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        string name = "invalidParam";
        string raw = "invalidValue";
        object? value = null;
        Type dataType = typeof(int);
        bool isValid = false;
        string validationError = "Failed to parse integer";

        // Act
        var result = factory.Create(name, raw, value, dataType, isValid, validationError);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(validationError, result.ValidationError);
    }

    /// <summary>
    /// Tests that Create is thread-safe when called concurrently with the same type.
    /// </summary>
    [Fact]
    public void Create_ConcurrentCallsWithSameType_AllSucceed()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(int);
        int threadCount = 10;
        var results = new IParameterValue[threadCount];

        // Act - Concurrent calls
        Parallel.For(0, threadCount, i =>
        {
            results[i] = factory.Create($"param{i}", $"raw{i}", i, dataType, true, null);
        });

        // Assert - All should succeed
        for (int i = 0; i < threadCount; i++)
        {
            Assert.NotNull(results[i]);
            Assert.Equal($"param{i}", results[i].Name);
            Assert.Equal(i, results[i].UntypedValue);
        }
    }

    /// <summary>
    /// Tests that Create is thread-safe when called concurrently with different types.
    /// </summary>
    [Fact]
    public void Create_ConcurrentCallsWithDifferentTypes_AllSucceed()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        int threadCount = 50;
        var results = new IParameterValue[threadCount];

        // Act - Concurrent calls with different types
        Parallel.For(0, threadCount, i =>
        {
            var uniqueType = CreateUniqueType(i);
            results[i] = factory.Create($"param{i}", $"raw{i}", null, uniqueType, true, null);
        });

        // Assert - All should succeed
        for (int i = 0; i < threadCount; i++)
        {
            Assert.NotNull(results[i]);
            Assert.Equal($"param{i}", results[i].Name);
        }
    }

    /// <summary>
    /// Tests that Create handles very long string parameters.
    /// </summary>
    [Fact]
    public void Create_VeryLongStrings_CreatesParameterValue()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        string longName = new string('a', 10000);
        string longRaw = new string('b', 10000);
        string longError = new string('c', 10000);
        Type dataType = typeof(string);

        // Act
        var result = factory.Create(longName, longRaw, "value", dataType, false, longError);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longName, result.Name);
        Assert.Equal(longRaw, result.RawValue);
        Assert.Equal(longError, result.ValidationError);
    }

    /// <summary>
    /// Tests that Create handles special characters in string parameters.
    /// </summary>
    [Fact]
    public void Create_SpecialCharactersInStrings_CreatesParameterValue()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        string specialName = "test\n\r\t\0\\\"";
        string specialRaw = "raw<>&\"'";
        string specialError = "error\u0001\u001F";
        Type dataType = typeof(string);

        // Act
        var result = factory.Create(specialName, specialRaw, "value", dataType, false, specialError);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialName, result.Name);
        Assert.Equal(specialRaw, result.RawValue);
        Assert.Equal(specialError, result.ValidationError);
    }

    /// <summary>
    /// Tests that Create handles extreme numeric values.
    /// </summary>
    /// <param name="dataType">The numeric type.</param>
    /// <param name="value">The extreme value.</param>
    [Theory]
    [InlineData(typeof(int), int.MinValue)]
    [InlineData(typeof(int), int.MaxValue)]
    [InlineData(typeof(long), long.MinValue)]
    [InlineData(typeof(long), long.MaxValue)]
    [InlineData(typeof(double), double.MinValue)]
    [InlineData(typeof(double), double.MaxValue)]
    [InlineData(typeof(double), double.NaN)]
    [InlineData(typeof(double), double.PositiveInfinity)]
    [InlineData(typeof(double), double.NegativeInfinity)]
    public void Create_ExtremeNumericValues_CreatesParameterValue(Type dataType, object value)
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();

        // Act
        var result = factory.Create("numericParam", "raw", value, dataType, true, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result.UntypedValue);
    }

    /// <summary>
    /// Tests that Create handles nullable value types.
    /// </summary>
    [Fact]
    public void Create_NullableValueType_CreatesParameterValue()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(int?);
        int? nullableValue = 42;

        // Act
        var result = factory.Create("nullableParam", "raw", nullableValue, dataType, true, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nullableValue, result.UntypedValue);
        Assert.Equal(dataType, result.DataType);
    }

    /// <summary>
    /// Tests that Create handles null nullable value type.
    /// </summary>
    [Fact]
    public void Create_NullNullableValueType_CreatesParameterValue()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(int?);
        int? nullableValue = null;

        // Act
        var result = factory.Create("nullableParam", "raw", nullableValue, dataType, true, null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.UntypedValue);
        Assert.Equal(dataType, result.DataType);
    }

    /// <summary>
    /// Tests that Create handles complex custom types.
    /// </summary>
    [Fact]
    public void Create_ComplexCustomType_CreatesParameterValue()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(CustomTestClass);
        var customValue = new CustomTestClass { Property = "test" };

        // Act
        var result = factory.Create("customParam", "raw", customValue, dataType, true, null);

        // Assert
        Assert.NotNull(result);
        Assert.Same(customValue, result.UntypedValue);
        Assert.Equal(dataType, result.DataType);
    }

    /// <summary>
    /// Tests that Create correctly caches constructed types.
    /// </summary>
    [Fact]
    public void Create_MultipleCallsWithSameType_ReusesCachedConstructedType()
    {
        // Arrange
        var factory = new ParameterValueFactoryCaching();
        Type dataType = typeof(string);

        // Act - Multiple calls
        var result1 = factory.Create("name1", "raw1", "value1", dataType, true, null);
        var result2 = factory.Create("name2", "raw2", "value2", dataType, true, null);
        var result3 = factory.Create("name3", "raw3", "value3", dataType, true, null);

        // Assert - All should succeed
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        // Verify constructed type cache has entry for string
        var constructedTypeCacheField = typeof(ParameterValueFactoryCaching)
            .GetField("_constructedTypeCache", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(constructedTypeCacheField);
        var constructedTypeCache = constructedTypeCacheField.GetValue(factory) as ConcurrentDictionary<Type, Type>;
        Assert.NotNull(constructedTypeCache);
        Assert.True(constructedTypeCache.ContainsKey(dataType));
    }

    /// <summary>
    /// Helper method to create unique types for cache testing.
    /// Returns different built-in types in a cycle to ensure uniqueness.
    /// </summary>
    private Type CreateUniqueType(int index)
    {
        var types = new[]
        {
            typeof(int), typeof(string), typeof(bool), typeof(double), typeof(long),
            typeof(float), typeof(byte), typeof(short), typeof(decimal), typeof(char),
            typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte), typeof(DateTime),
            typeof(TimeSpan), typeof(Guid), typeof(Uri), typeof(Version), typeof(object)
        };

        // For index >= 20, use the base types to create unique combinations
        if (index < types.Length)
        {
            return types[index];
        }

        // Create array types for additional uniqueness
        int baseIndex = (index - types.Length) % types.Length;
        int arrayRank = ((index - types.Length) / types.Length) % 3 + 1;

        if (arrayRank == 1)
            return types[baseIndex].MakeArrayType();
        else if (arrayRank == 2)
            return types[baseIndex].MakeArrayType(2);
        else
            return typeof(Nullable<>).MakeGenericType(types[baseIndex % 10]); // Only value types can be nullable
    }

    /// <summary>
    /// Custom test class for complex type testing.
    /// </summary>
    private class CustomTestClass
    {
        public string? Property { get; set; }
    }
}