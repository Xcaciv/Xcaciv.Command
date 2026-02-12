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
        public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            if (parameters.TryGetValue("text", out var textParam))
            {
                return CommandResult<string>.Success(parameters["text"].GetValue<string>());
            }
            return CommandResult<string>.Success(String.Join(' ', parameters.Values.Select(p => p.GetValue<string>())));
        }

        public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            return CommandResult<string>.Success(pipedChunk.Output);
        }
    }
}
