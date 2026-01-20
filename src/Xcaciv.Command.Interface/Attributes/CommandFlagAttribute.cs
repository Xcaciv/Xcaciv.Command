using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface.Attributes
{
    /// <summary>
    /// this attribute indicates a toggle flag for a command
    /// if the flag is present the value is true
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CommandFlagAttribute : AbstractCommandParameterAttribute
    {
        /// <summary>
        /// this attribute indicates a toggle flag for a command
        /// if the flag is present the value is true
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        public CommandFlagAttribute(string name, string description) 
        { 
            this.Name = name;
            this.ValueDescription = description;
            this.Indication = ParameterIndication.FLAG;
        }

        public override string GetIndicator()
        {
            return $"-{_helpName}";
        }

        public override string GetValueDescription()
        {
            return $"Flag: {ValueDescription}";
        }

    }
}
