using System;

namespace Xcaciv.Command.Interface.Parameters;

/// <summary>
/// Default parameter value factory without caching.
/// Provides predictable memory usage with reflection overhead on each call.
/// Uses direct reflection (MakeGenericType + Activator.CreateInstance) for each parameter value creation.
/// </summary>
public class ParameterValueFactory : IParameterValueFactory
{
    /// <summary>
    /// Creates a typed parameter value using reflection.
    /// No caching; each call performs MakeGenericType and Activator.CreateInstance.
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

        var constructedType = typeof(ParameterValue<>).MakeGenericType(dataType);
        var instance = Activator.CreateInstance(constructedType, name, raw, value, isValid, validationError)
                       ?? throw new InvalidOperationException($"Failed to create ParameterValue for type {dataType.Name}.");

        return (IParameterValue)instance;
    }
}
