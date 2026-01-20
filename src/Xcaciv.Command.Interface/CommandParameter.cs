using System;
using System.Collections.Generic;
using System.Text;

namespace Xcaciv.Command.Interface
{
    public class CommandParameter : ICommandParameter
    {
        public bool IsRequired { get; init; } = false;

        public ParameterIndication Indication { get; init; } = ParameterIndication.NAMED;

        public string Name { get; init; } = String.Empty;

        public string ValueDescription { get; init; } = String.Empty;

        /// <summary>
        /// used when no value is provided
        /// this satisfies the IsRequired flag
        /// </summary>
        public string DefaultValue
        {
            get => _defaultValue;
            set
            {
                ValidateDefaultValue(value, _allowedValues);
                _defaultValue = value;
            }
        }

        public Type DataType { get; init; } = typeof(object);

        /// <summary>
        /// input values that are allowed, anything else will throw an error
        /// case is ignored
        /// </summary>
        public string[] AllowedValues
        {
            get => _allowedValues;
            init
            {
                _allowedValues = value ?? [];

                // Auto-set default value to first allowed value if not already set
                if (_allowedValues.Length > 0 && string.IsNullOrEmpty(_defaultValue))
                {
                    _defaultValue = _allowedValues[0];
                }

                // Validate existing default value against new allowed values
                if (!string.IsNullOrEmpty(_defaultValue))
                {
                    ValidateDefaultValue(_defaultValue, _allowedValues);
                }
            }
        }

        private void ValidateDefaultValue(string defaultValue, string[] allowedValues)
        {
            if (string.IsNullOrEmpty(defaultValue) || allowedValues == null || allowedValues.Length == 0)
            {
                return;
            }

            if (!allowedValues.Contains(defaultValue, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Default value '{defaultValue}' is not in the allowed values list for parameter '{Name}'. " +
                    $"Allowed values: {string.Join(", ", allowedValues)}");
            }
        }

        protected string[] _allowedValues = Array.Empty<string>();

        protected string _defaultValue = String.Empty;

        public string ShortAlias { get; init; } = String.Empty;

        public string CommandPrototype { get; init; } = String.Empty;

        public bool UsePipe { get; init; } = false;
    }
}
