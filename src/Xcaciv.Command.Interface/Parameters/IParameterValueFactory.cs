using System;

namespace Xcaciv.Command.Interface.Parameters;

/// <summary>
/// Factory interface for creating typed parameter values.
/// Allows swapping between non-caching (default) and caching implementations.
/// </summary>
public interface IParameterValueFactory
{
    /// <summary>
    /// Creates a typed parameter value instance.
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="raw">Raw string value</param>
    /// <param name="value">Converted/validated value</param>
    /// <param name="dataType">Target type for generic instantiation</param>
    /// <param name="isValid">Whether validation succeeded</param>
    /// <param name="validationError">Validation error message if any</param>
    /// <returns>Typed parameter value instance</returns>
    IParameterValue Create(string name, string raw, object? value, Type dataType, bool isValid, string? validationError);
}
