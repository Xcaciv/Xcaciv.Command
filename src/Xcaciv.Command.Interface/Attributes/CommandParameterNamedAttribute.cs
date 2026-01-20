using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CommandParameterNamedAttribute : AbstractCommandParameterAttribute
    {
        public CommandParameterNamedAttribute(string name, string description) 
        { 
            this.Name = name;
            this.ValueDescription = description;
        }

        public override string GetIndicator()
        {
            return $"-{_helpName}";
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
