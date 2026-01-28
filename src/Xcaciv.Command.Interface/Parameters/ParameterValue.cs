using System;

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
}
