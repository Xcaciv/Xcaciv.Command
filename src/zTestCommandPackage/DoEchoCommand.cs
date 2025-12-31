using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace zTestCommandPackage
{
    [CommandRoot("do", "does stuff")]
    [CommandRegister("ECHO", "SUB DO echo")]
    [CommandParameterSuffix("text", "Text to echo")]
    public class DoEchoCommand : AbstractCommand, ICommandDelegate
    {
        public override string HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            if (parameters.TryGetValue("text", out var textParam))
            {
                return textParam.RawValue;
            }
            return String.Join(' ', parameters.Values.Select(p => p.RawValue));
        }

        public override string HandlePipedChunk(string pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            return pipedChunk;
        }
    }
}
