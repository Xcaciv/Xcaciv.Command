using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests.Commands
{
    [CommandRoot("Package", "Package commands")]
    [CommandRegister("Install", "install a package")]
    [CommandParameterOrdered("packagename", "The unique name of the package to install", IsRequired = true)]
    public class InstallCommand : AbstractCommand
    {
        public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            var paramNames = string.Join(',', parameters.Keys);
            return CommandResult<string>.Success("Not installing " + paramNames);
        }

        public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            var paramNames = string.Join(',', parameters.Keys);
            return CommandResult<string>.Success($"Not installing {pipedChunk.Output} " + paramNames);
        }
    }
}
