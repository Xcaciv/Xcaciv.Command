using System;
using System.Collections.Concurrent;

namespace Xcaciv.Command.Interface.Parameters
{
    /// <summary>
    /// Strongly-typed parameter value backed by boxed storage to allow invalid sentinels.
    /// </summary>
    public class ParameterValue<T> : AbstractParameterValue<T>
    {
        public ParameterValue(string name, string raw, object? value, bool isValid, string? validationError)
            : base(name, raw, value, isValid, validationError)
        {
        }
    }

    /// <summary>
    /// Factory for creating typed parameter values when the target type is only known at runtime.
    /// Optimized with caching to reduce reflection overhead.
    /// </summary>
    public static class ParameterValue
    {
        /// <summary>
        /// Cache for constructed generic types to avoid repeated MakeGenericType calls.
        /// Thread-safe for concurrent command execution.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Type> ConstructedTypeCache = new();

        /// <summary>
        /// Cache for factory delegates to avoid repeated Activator.CreateInstance calls.
        /// Thread-safe for concurrent command execution.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<string, string, object?, bool, string?, IParameterValue>> FactoryCache = new();

        public static IParameterValue Create(string name, string raw, object? value, Type dataType, bool isValid, string? validationError)
        {
            if (dataType == null)
                throw new ArgumentNullException(nameof(dataType));

            // Fast path: check factory cache first
            if (FactoryCache.TryGetValue(dataType, out var cachedFactory))
            {
                return cachedFactory(name, raw, value, isValid, validationError);
            }

            // Slow path: create factory delegate and cache it
            var constructedType = ConstructedTypeCache.GetOrAdd(dataType, dt =>
                typeof(ParameterValue<>).MakeGenericType(dt));

            // Create factory delegate that captures the constructed type
            var factory = (string n, string r, object? v, bool valid, string? error) =>
            {
                var instance = Activator.CreateInstance(constructedType, n, r, v, valid, error)
                               ?? throw new InvalidOperationException($"Failed to create ParameterValue for type {dataType.Name}.");
                return (IParameterValue)instance;
            };

            // Cache factory (bounded: max 100 types to prevent unbounded growth)
            if (FactoryCache.Count < 100)
            {
                FactoryCache.TryAdd(dataType, factory);
            }

            return factory(name, raw, value, isValid, validationError);
        }
    }
}
