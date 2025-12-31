using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Core.Parameters;

/// <summary>
/// Builds parameter collections from parsed string parameters with pre-validation.
/// Parameters are validated during construction and invalid parameters throw exceptions.
/// </summary>
public class ParameterCollectionBuilder
{
    private readonly IParameterConverter _converter;

    /// <summary>
    /// Creates a new parameter collection builder.
    /// </summary>
    /// <param name="converter">The converter to use for type conversion. Defaults to DefaultParameterConverter.</param>
    public ParameterCollectionBuilder(IParameterConverter? converter = null)
    {
        _converter = converter ?? new DefaultParameterConverter();
    }

    /// <summary>
    /// Builds a parameter collection from parsed string parameters with full validation.
    /// Invalid parameters throw exceptions immediately.
    /// </summary>
    /// <param name="parametersDict">Dictionary of parameter name-value pairs (as strings).</param>
    /// <param name="attributes">The parameter attribute definitions.</param>
    /// <returns>A collection of validated parameter values.</returns>
    /// <exception cref="ArgumentException">If any parameter fails validation.</exception>
    public ParameterCollection Build(Dictionary<string, string> parametersDict, AbstractCommandParameter[] attributes)
    {
        if (parametersDict == null)
            throw new ArgumentNullException(nameof(parametersDict));

        var collection = new ParameterCollection();
        var errors = new List<string>();

        foreach (var attr in (attributes ?? Array.Empty<AbstractCommandParameter>())
            .Where(a => parametersDict.ContainsKey(a.Name)))
        {
            var rawValue = parametersDict[attr.Name];
            var paramValue = new ParameterValue(attr.Name, rawValue, attr.DataType, _converter);
            
            // Validate before adding
            if (!paramValue.IsValid)
            {
                errors.Add($"Parameter '{attr.Name}': {paramValue.ValidationError}");
                continue;
            }

            collection[attr.Name] = paramValue;
        }

        // If there are any validation errors, throw a single exception with all details
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Parameter validation failed:\n" + string.Join("\n", errors.Select(e => $"  - {e}")));
        }

        return collection;
    }

    /// <summary>
    /// Builds a parameter collection with validation that throws on first error.
    /// Useful for strict validation scenarios where partial results are not acceptable.
    /// </summary>
    /// <param name="parametersDict">Dictionary of parameter name-value pairs.</param>
    /// <param name="attributes">The parameter attribute definitions.</param>
    /// <returns>A collection of validated parameter values.</returns>
    /// <exception cref="ArgumentException">If any parameter fails validation (throws on first error).</exception>
    public ParameterCollection BuildStrict(Dictionary<string, string> parametersDict, AbstractCommandParameter[] attributes)
    {
        if (parametersDict == null)
            throw new ArgumentNullException(nameof(parametersDict));

        var collection = new ParameterCollection();

        foreach (var attr in (attributes ?? Array.Empty<AbstractCommandParameter>())
            .Where(a => parametersDict.ContainsKey(a.Name)))
        {
            var rawValue = parametersDict[attr.Name];
            var paramValue = new ParameterValue(attr.Name, rawValue, attr.DataType, _converter);
            
            // Fail fast on first validation error
            if (!paramValue.IsValid)
            {
                throw new ArgumentException(
                    $"Parameter '{attr.Name}' validation failed: {paramValue.ValidationError}");
            }

            collection[attr.Name] = paramValue;
        }

        return collection;
    }
}
