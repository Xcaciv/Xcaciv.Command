using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Commands
{
    [CommandRegister("REGIF", "Regular expression filter. Outputs the string if it matches", Prototype = @"<some command> | regif ""<regex expression>"" ""<string to check>""")]
    [CommandParameterOrdered("Regex", "Regular Expression")]
    [CommandParameterOrdered("String", "String to match", UsePipe=true)]
    public class RegifCommand : AbstractCommand
    {
        /// <summary>
        /// regex object for reuse
        /// </summary>
        protected Regex? expression { get; set; } = null;

        protected string regex { get; set; } = string.Empty;

        public override string HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext status)
        {
            var output = new StringBuilder();
            
            if (parameters.TryGetValue("regex", out var regexParam) && regexParam.IsValid)
            {
                this.expression = new Regex(regexParam.RawValue);
                
                // Check if there's a string parameter
                if (parameters.TryGetValue("string", out var stringParam) && 
                    stringParam.IsValid && 
                    this.expression.IsMatch(stringParam.RawValue))
                {
                    output.Append(stringParam.RawValue);
                }
            }
            return output.ToString().Trim();
        }

        public override string HandlePipedChunk(string stringToCheck, Dictionary<string, IParameterValue> parameters, IEnvironmentContext status)
        {
            if (parameters.TryGetValue("regex", out var regexParam) && regexParam.IsValid)
            {
                if (this.expression == null)
                {
                    this.expression = new Regex(regexParam.RawValue);
                }

                return (this.expression?.IsMatch(stringToCheck) ?? false) ? stringToCheck : string.Empty;
            }

            return string.Empty;
        }
        /// <summary>
        /// setup the regex object
        /// </summary>
        /// <param name="parameters"></param>
        private void setRegexExpression(string[] parameters)
        {
            if (this.expression == null && parameters.Length >0)
            {
                this.expression = new Regex(parameters[0]);
            }
        }


    }
}
