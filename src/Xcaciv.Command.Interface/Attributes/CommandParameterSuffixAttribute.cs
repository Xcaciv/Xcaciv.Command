using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CommandParameterSuffixAttribute : AbstractCommandParameterAttribute
    {
        public CommandParameterSuffixAttribute(string name, string description) 
        { 
            this.Name = name;
            this.ValueDescription = description;
            
            this.Indication = ParameterIndication.SUFFIX;
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
