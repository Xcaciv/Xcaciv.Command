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
    [CommandRegister("search", "Search for command packages using natural language terms")]
    [CommandRoot("package", "Manage command packages")]
    [CommandParameterOrdered("terms", "Search terms", UsePipe = true)]
    [CommandParameterNamed("take", "Number of results to return (default 20, max 1000)", DataType = typeof(int), DefaultValue = "200")]
    [CommandFlag("prerelease", "Include prerelease packages", DataType = typeof(bool))]
    [CommandParameterNamed("source", "Package source URL (HTTPS)")]
    [CommandParameterNamed("verbosity", "quiet|normal|detailed", AllowedValues = ["quiet", "normal", "detailed"])]
    public class TestSubCommand : AbstractCommand
    {
        private StringBuilder outputBuffer = new StringBuilder();
        public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            var output = formatParameters(parameters);

            return CommandResult<string>.Success(output);
        }

        private static string formatParameters(Dictionary<string, IParameterValue> parameters)
        {
            var output = new StringBuilder($"\n## Parameters\n\n");
            output.Append("| Key | Name | DataType | Valid | Value |\n|---------|--------|-------|-------|\n");

            foreach (var parameter in parameters)
            {
                var key = parameter.Key;
                var value = parameter.Value;
                var parameterType = value.DataType;
                output.Append($"| {key} |{value.Name} | {value.DataType.Name} | {value.IsValid} | {value.RawValue}|\n");
            }

            return output.ToString();
        }

        protected override void OnStartPipe(Dictionary<string, IParameterValue> processedParameters, IEnvironmentContext environment)
        {
            outputBuffer = new StringBuilder(formatParameters(processedParameters));
            outputBuffer.Append($"\n\n## Piped Chunk\n\n");
            outputBuffer.Append("| Success | Format | Value |\n|---------|--------|-------|\n");

        }

        public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            outputBuffer.Append($"|{pipedChunk.IsSuccess}|{pipedChunk.OutputFormat}|{pipedChunk.Output}\n");
            return CommandResult<string>.Success(outputBuffer.ToString());
        }
    }
}
