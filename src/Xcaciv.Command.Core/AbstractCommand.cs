using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Core
{
    public abstract class AbstractCommand : ICommandDelegate
    {
        private static IHelpService? _helpService;

        /// <summary>
        /// Sets the help service used by all AbstractCommand instances for help formatting.
        /// Should be set during application startup, typically by the CommandController.
        /// </summary>
        public static void SetHelpService(IHelpService helpService)
        {
            _helpService = helpService ?? throw new ArgumentNullException(nameof(helpService));
        }

        /// <summary>
        /// this should be overwritten to dispose of any unmanaged items
        /// </summary>
        /// <returns></returns>
        public virtual ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// output full help
        /// </summary>
        /// <param name="parameters"></param>
        public virtual string Help(string[] parameters, IEnvironmentContext env)
        {
            if (_helpService != null)
            {
                return _helpService.BuildHelp(this, parameters, env);
            }

            return BuildHelpString(parameters, env);
        }

        /// <summary>
        /// single line help command description, used for listing all commands
        /// </summary>
        /// <param name="parameters"></param>
        public virtual string OneLineHelp(string[] parameters)
        {
            var thisType = GetType();
            var baseCommand = Attribute.GetCustomAttribute(thisType, typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;
            if (baseCommand == null)
            {
                throw new Exception("CommandRegisterAttribute is required for all commands");
            }

            if (Attribute.GetCustomAttribute(thisType, typeof(CommandRootAttribute)) is CommandRootAttribute)
            {
                return $"-\t{baseCommand.Command,-12} {baseCommand.Description}";
            }
            else
            {
                return $"{baseCommand.Command,-12} {baseCommand.Description}";
            }
        }

        /// <summary>
        /// [Obsolete] Legacy help builder. Use IHelpService.BuildHelp() instead.
        /// Kept for backward compatibility with commands that override this method.
        /// </summary>
        [Obsolete("Use IHelpService.BuildHelp() instead. This method will be removed in v3.0.")]
        protected virtual string BuildHelpString(string[] parameters, IEnvironmentContext environment)
        {
            var thisType = GetType();
            var baseCommand = Attribute.GetCustomAttribute(thisType, typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;
            var commandParametersOrdered = GetOrderedParameters(false);
            var commandParametersFlag = GetFlagParameters();
            var commandParametersNamed = GetNamedParameters(false);
            var commandParametersSuffix = GetSuffixParameters(false);
            var helpRemarks = Attribute.GetCustomAttributes(thisType, typeof(CommandHelpRemarksAttribute)) as CommandHelpRemarksAttribute[];

            var builder = new StringBuilder();
            if (Attribute.GetCustomAttribute(thisType, typeof(CommandRootAttribute)) is CommandRootAttribute rootCommand)
                builder.Append($"{rootCommand.Command} ");            
            builder.AppendLine($"{baseCommand?.Command}:");
            builder.AppendLine($"  {baseCommand?.Description}");
            builder.AppendLine();
            builder.AppendLine($"Usage:");
            
            if (commandParametersOrdered.Length + commandParametersNamed.Length + commandParametersSuffix.Length + commandParametersFlag.Length > 0)
            {
                var parameterBuilder = new StringBuilder();
                var prototypeBuilder = new StringBuilder();

                foreach (var parameter in commandParametersOrdered)
                {
                    parameterBuilder.AppendLine($"  {parameter}");
                    prototypeBuilder.Append($"{parameter.GetIndicator()} ");
                }

                foreach (var parameter in commandParametersFlag)
                {
                    parameterBuilder.AppendLine($"  {parameter}");
                    prototypeBuilder.Append($"{parameter.GetIndicator()} ");
                }

                foreach (var parameter in commandParametersNamed)
                {
                    parameterBuilder.AppendLine($"  {parameter}");
                    string valueIndicator = (parameter.AllowedValues.Length > 0) ?
                        $"[{string.Join("|", parameter.AllowedValues)}]" :
                        parameter.Name;

                    prototypeBuilder.Append($"{parameter.GetIndicator()} <{valueIndicator}> ");
                }

                foreach (var parameter in commandParametersSuffix)
                {
                    parameterBuilder.AppendLine($"  {parameter}");
                    prototypeBuilder.Append($"{parameter.GetIndicator()} ");
                }

                if (baseCommand?.Prototype.Equals("todo", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    builder.AppendLine(prototypeBuilder.ToString());
                }
                else
                {
                    builder.AppendLine($"  {baseCommand?.Prototype}");
                }

                builder.AppendLine();
                builder.AppendLine("Options:");
                builder.AppendLine(parameterBuilder.ToString());
            }

            if (helpRemarks != null && helpRemarks.Length > 0)
            {
                builder.AppendLine("Remarks:");
                foreach (var rem in helpRemarks)
                {
                    builder.AppendLine();
                    builder.AppendLine(rem.Remarks);
                }
            }

            builder.AppendLine();
            return builder.ToString();
        }

        /// <summary>
        /// execute pipe and single input the same
        /// </summary>
        /// <param name="io"></param>
        /// <param name="environment"></param>
        /// <returns></returns>
        public virtual async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            var processedParameters = ProcessParameters(io.Parameters, io.HasPipedInput);

            if (io.HasPipedInput)
            {
                OnStartPipe(processedParameters, environment);

                await foreach (var pipedChunk in io.ReadInputPipeChunks())
                {
                    if (string.IsNullOrEmpty(pipedChunk)) continue;
                    yield return CommandResult<string>.Success(HandlePipedChunk(pipedChunk, processedParameters, environment));
                }

                OnEndPipe(processedParameters, environment);

            }
            else
            {
                var parameterArray = io.Parameters ?? Array.Empty<string>();
                var isHelp = _helpService?.IsHelpRequest(parameterArray) ?? false;
                if (isHelp)
                {
                    yield return CommandResult<string>.Success(Help(parameterArray, environment));
                }
                else
                {
                    yield return CommandResult<string>.Success(HandleExecution(processedParameters, environment));
                }
            }
        }

        /// <summary>
        /// Use the parameter attributes to process the parameters into a typed dictionary
        /// </summary>
        /// <param name="parameters"></param>
        public Dictionary<string, IParameterValue> ProcessParameters(string[] parameters, bool hasPipedInput = false)
        {
            if (parameters.Length == 0) return new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);

            return CommandParameters.ProcessTypedParameters(
                parameters,
                GetOrderedParameters(hasPipedInput),
                GetFlagParameters(),
                GetNamedParameters(hasPipedInput),
                GetSuffixParameters(hasPipedInput));
        }

        /// <summary>
        /// reads parameter description from the instance
        /// </summary>
        /// <returns></returns>
        protected virtual CommandParameterOrderedAttribute[] GetOrderedParameters(bool hasPipedInput)
        {
            var thisType = GetType();
            var ordered = Attribute.GetCustomAttributes(thisType, typeof(CommandParameterOrderedAttribute)) as CommandParameterOrderedAttribute[] ?? ([]);
            if (hasPipedInput)
            {
                ordered = ordered.Where(x => !x.UsePipe).ToArray();
            }

            return ordered;
        }

        /// <summary>
        /// reads parameter description from the instance
        /// </summary>
        /// <returns></returns>
        protected virtual CommandParameterNamedAttribute[] GetNamedParameters(bool hasPipedInput)
        {
            var thisType = GetType();
            var named = Attribute.GetCustomAttributes(thisType, typeof(CommandParameterNamedAttribute)) as CommandParameterNamedAttribute[] ?? ([]);
            if (hasPipedInput)
            {
                named = named.Where(x => !x.UsePipe).ToArray();
            }
            return named;
        }

        /// <summary>
        /// reads parameter description from the instance
        /// </summary>
        /// <returns></returns>
        protected virtual CommandFlagAttribute[] GetFlagParameters()
        {
            var thisType = GetType();
            var flags = Attribute.GetCustomAttributes(thisType, typeof(CommandFlagAttribute)) as CommandFlagAttribute[] ?? ([]);
            return flags;
        }

        /// <summary>
        /// reads parameter description from the instance
        /// </summary>
        /// <returns></returns>
        protected virtual CommandParameterSuffixAttribute[] GetSuffixParameters(bool hasPipedInput)
        {
            var thisType = GetType();
            var flags = Attribute.GetCustomAttributes(thisType, typeof(CommandParameterSuffixAttribute)) as CommandParameterSuffixAttribute[] ?? ([]);
            if (hasPipedInput)
            {
                flags = flags.Where(x => !x.UsePipe).ToArray();
            }
            return flags;
        }

        public abstract string HandlePipedChunk(string pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env);

        /// <summary>
        /// Invoked when a pipe operation is starting, allowing derived classes to perform custom initialization or
        /// setup.
        /// </summary>
        /// <param name="processedParameters">A dictionary containing the processed parameter values for the pipe operation. Keys represent parameter
        /// names; values provide the corresponding parameter values. Cannot be null.</param>
        /// <param name="environment">The environment context in which the pipe is being started. Provides access to environment-specific
        /// information and services. Cannot be null.</param>
        protected virtual void OnStartPipe(Dictionary<string, IParameterValue> processedParameters, IEnvironmentContext environment)
        {
            // handle start for pipes
        }
        /// <summary>
        /// Invoked when the pipe processing has completed, allowing for custom post-processing or cleanup.
        /// </summary>
        /// <remarks>Override this method in a derived class to implement custom logic that should run
        /// after the pipe has finished processing. This method is called after all parameters have been
        /// processed.</remarks>
        /// <param name="processedParameters">A dictionary containing the parameters that were processed during the pipe execution. Keys represent
        /// parameter names; values are the corresponding processed values.</param>
        /// <param name="environment">The environment context in which the pipe was executed. Provides access to environment-specific information
        /// and services.</param>
        protected virtual void OnEndPipe(Dictionary<string, IParameterValue> processedParameters, IEnvironmentContext environment)
        {
            // handle end for pipes
        }

        public abstract string HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env);
    }
}
