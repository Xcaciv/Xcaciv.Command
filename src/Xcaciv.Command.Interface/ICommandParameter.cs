using System;
using System.Collections.Generic;
using System.Text;

namespace Xcaciv.Command.Interface
{
    /// <summary>
    /// data object to describe a parameter to a commend
    /// used in filtering and validation as well as help generation
    /// </summary>
    public interface ICommandParameter
    {
        /// <summary>
        /// How the parameter is spedcified in the command string
        /// </summary>
        ParameterIndication Indication { get; }
        /// <summary>
        /// Indicates an exception shoudl be thrown if no value was specified in the command string
        /// </summary>
        bool IsRequired { get; }
        /// <summary>
        /// text used to identify the parameter
        /// expected to be alphanumeric and no spaces
        /// </summary>
        string Name { get; }
        /// <summary>
        /// describe how the value is used so the user can understand
        /// </summary>
        string ValueDescription { get; }
        /// <summary>
        /// This is the expected data type. If the string input cannot be converted to this type, an exception is thrown
        /// </summary>
        Type DataType { get; }
        /// <summary>
        /// Gets a default value when no value is required
        /// </summary>
        string DefaultValue { get; }
        /// <summary>
        /// input values that are allowed, anything else will throw an error
        /// case should be ignored
        /// </summary>
        string[] AllowedValues { get;  }
        /// <summary>
        /// while a parameter has a primary full name, it can also have a short alias
        /// eg. -u for -username
        /// </summary>
        string ShortAlias { get; }
        /// <summary>
        /// Gets the prototype or template string that defines the expected format of the command.
        /// eg: "SET <varname> <value>"
        /// </summary>
        string CommandPrototype { get; }
        /// <summary>
        /// this flag determines whether the parameter can accept piped input
        /// There can only be one parameter with this flag set to true per command
        /// </summary>
        bool UsePipe { get; }
    }
}
