using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests.Commands
{
    /// <summary>
    /// Test command that uses constructor-provided parameter attributes for testing parameter processing.
    /// This command uses reflection to add attributes dynamically at runtime for testing purposes.
    /// </summary>
    [CommandRegister("PARAMTEST", "Test command for parameter processing")]
    public class ParameterTestCommand : AbstractCommand
    {
        private readonly CommandParameterOrderedAttribute[] _ordered;
        private readonly CommandFlagAttribute[] _flags;
        private readonly CommandParameterNamedAttribute[] _named;
        private readonly CommandParameterSuffixAttribute[] _suffix;

        public ParameterTestCommand(
            CommandParameterOrderedAttribute[] ordered,
            CommandFlagAttribute[] flags,
            CommandParameterNamedAttribute[] named,
            CommandParameterSuffixAttribute[] suffix)
        {
            _ordered = ordered;
            _flags = flags;
            _named = named;
            _suffix = suffix;
        }

        public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            var builder = new StringBuilder();

            foreach (var pair in parameters)
            {
                var valueStr = pair.Value.DataType == typeof(bool)
                    ? pair.Value.GetValue<bool>().ToString().ToLowerInvariant()
                    : pair.Value.RawValue?.ToString() ?? string.Empty;
                
                builder.AppendLine($"{pair.Key} = {valueStr}");
            }

            return CommandResult<string>.Success(builder.ToString(), this.OutputFormat);
        }

        public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Override ProcessParameters to use the constructor-provided attributes instead of reflection.
        /// This allows tests to inject custom parameter definitions without using actual attributes on the class.
        /// </summary>
        public Dictionary<string, IParameterValue> ProcessParameters(string[] parameters, bool hasPipedInput = false)
        {
            if (parameters.Length == 0)
            {
                return new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            }

            var orderedAttrs = _ordered ?? Array.Empty<CommandParameterOrderedAttribute>();
            var namedAttrs = _named ?? Array.Empty<CommandParameterNamedAttribute>();
            var suffixAttrs = _suffix ?? Array.Empty<CommandParameterSuffixAttribute>();

            if (hasPipedInput)
            {
                orderedAttrs = orderedAttrs.Where(x => !x.UsePipe).ToArray();
                namedAttrs = namedAttrs.Where(x => !x.UsePipe).ToArray();
                suffixAttrs = suffixAttrs.Where(x => !x.UsePipe).ToArray();
            }

            var commandParameters = new CommandParameters();
            return commandParameters.ProcessParameters(
                parameters,
                orderedAttrs,
                _flags ?? Array.Empty<CommandFlagAttribute>(),
                namedAttrs,
                suffixAttrs);
        }
    }
}
