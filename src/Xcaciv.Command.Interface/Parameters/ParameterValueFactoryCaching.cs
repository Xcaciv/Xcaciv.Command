using System;
using System.Collections.Concurrent;

namespace Xcaciv.Command.Interface.Parameters;

/// <summary>
/// Caching parameter value factory for high-performance scenarios.
/// Reduces reflection overhead by caching constructed types and factory delegates.
/// Memory overhead: ~10-30 KB for cache (bounded to 100 type entries).
/// Thread-safe for concurrent command execution.
/// </summary>
public class ParameterValueFactoryCaching : IParameterValueFactory
{
    /// <summary>
    /// Cache for constructed generic types to avoid repeated MakeGenericType calls.
    /// Thread-safe for concurrent command execution.
    /// </summary>
    private readonly ConcurrentDictionary<Type, Type> _constructedTypeCache = new();

    /// <summary>
    /// Cache for factory delegates to avoid repeated Activator.CreateInstance calls.
    /// Thread-safe for concurrent command execution.
    /// </summary>
    private readonly ConcurrentDictionary<Type, Func<string, string, object?, bool, string?, IParameterValue>> _factoryCache = new();

    /// <summary>
    /// Creates a typed parameter value with caching for improved performance.
    /// First call for a type uses reflection; subsequent calls use cached delegate.
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="raw">Raw string value</param>
    /// <param name="value">Converted/validated value</param>
    /// <param name="dataType">Target type for generic instantiation</param>
    /// <param name="isValid">Whether validation succeeded</param>
    /// <param name="validationError">Validation error message if any</param>
    /// <returns>Typed parameter value instance</returns>
    public IParameterValue Create(string name, string raw, object? value, Type dataType, bool isValid, string? validationError)
    {
        if (dataType == null)
            throw new ArgumentNullException(nameof(dataType));

        // Fast path: check factory cache first
        if (_factoryCache.TryGetValue(dataType, out var cachedFactory))
        {
            return cachedFactory(name, raw, value, isValid, validationError);
        }

        // Slow path: create factory delegate and cache it
        var constructedType = _constructedTypeCache.GetOrAdd(dataType, dt =>
            typeof(ParameterValue<>).MakeGenericType(dt));

        // Create factory delegate that captures the constructed type
        var factory = (string n, string r, object? v, bool valid, string? error) =>
        {
            var instance = Activator.CreateInstance(constructedType, n, r, v, valid, error)
                           ?? throw new InvalidOperationException($"Failed to create ParameterValue for type {dataType.Name}.");
            return (IParameterValue)instance;
        };

        // Cache factory (bounded: max 100 types to prevent unbounded growth)
        if (_factoryCache.Count < 100)
        {
            _factoryCache.TryAdd(dataType, factory);
        }

        return factory(name, raw, value, isValid, validationError);
    }

    /// <summary>
    /// Clears the type and factory caches. Useful for testing or memory management.
    /// </summary>
    public void ClearCache()
    {
        _constructedTypeCache.Clear();
        _factoryCache.Clear();
    }
}
