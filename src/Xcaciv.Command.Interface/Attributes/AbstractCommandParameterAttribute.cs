using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface.Attributes
{
    public abstract class AbstractCommandParameterAttribute : Attribute, ICommandParameter
    {
        public ParameterIndication Indication { get; init; } = ParameterIndication.NAMED;
        public bool IsRequired { get; init; } = false;

        protected string _helpName = "TODO";
        /// <summary>
        /// description of the value for help
        /// </summary>
        public string ValueDescription { get; set; }  = String.Empty;

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
        /// <summary>
        /// even though this parameter does not require a name, it is used for help
        /// </summary>
        public string Name
        {
            get { return _helpName; }
            set { _helpName = CommandNameValidator.GetValidCommandName(value, false); }
        }
        /// <summary>
        /// Data type of the parameter
        /// </summary>
        public Type DataType { get; set; } = typeof(string);


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

        public string ShortAlias { get; set; }  = String.Empty;

        public string CommandPrototype { get; set; }  = String.Empty;

        public bool UsePipe { get; set; } = false;

        /// <summary>
        /// format the help string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string indicator = GetIndicator();
            string valueDescription = GetValueDescription();

            return $"{indicator,-18} {valueDescription}".Trim();
        }

        public virtual string GetIndicator()
        {
            return $"<{_helpName}>";
        }

        public virtual string GetValueDescription()
        {
            return ValueDescription;
        }
    }
}
