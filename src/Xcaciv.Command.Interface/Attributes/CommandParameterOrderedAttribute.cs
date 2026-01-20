using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface.Attributes
{
    /// <summary>
    /// An unnamed parameter that is determined by the order of the value in the parameters
    /// ordered parameters are required to be passed before named parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CommandParameterOrderedAttribute : AbstractCommandParameterAttribute
    {
        /// <summary>
        /// An unnamed parameter that is determined by the order of the value in the parameters
        /// ordered parameters are required to be passed before named parameters
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        public CommandParameterOrderedAttribute(string name, string description) 
        { 
            this.Name = name;
            this.ValueDescription = description;
            this.IsRequired = true;

            this.Indication = ParameterIndication.ORDERED;
        }
        public override string GetValueDescription()
        {
            string description = ValueDescription;
            if (AllowedValues.Length > 0)
            {
                description += $" (Allowed values: {string.Join(", ", AllowedValues)})";
            }
            return description;
        }
    }
}
