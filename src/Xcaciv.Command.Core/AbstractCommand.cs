using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Core
{
    public abstract class AbstractCommand : ICommandDelegate
    {
        private static IHelpService? _helpService;

        public ResultFormat OutputFormat { get; protected set; } = ResultFormat.General;
        protected string _command = string.Empty;
        private string _rootCommand = string.Empty;

        public string Command
        {
            get
            {
                if (String.IsNullOrEmpty(_command))
                {
                    var thisType = GetType();
                    var registration = Attribute.GetCustomAttribute(thisType, typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;
                    if (registration == null)
                    {
                        throw new InvalidOperationException("CommandRegisterAttribute is required for all commands");
                    }
                    _command = registration.Command;
                }
                return _command;
            }
            set
            {
                _command = NamesValidator.GetValidCommandName(value);
            }
        }

        public string RootCommand
        {
            get
            {
                if (String.IsNullOrEmpty(_rootCommand))
                {
                    var thisType = GetType();
                    var registration = Attribute.GetCustomAttribute(thisType, typeof(CommandRootAttribute)) as CommandRootAttribute;
                    if (registration == null)
                    {
                        throw new InvalidOperationException("CommandRegisterAttribute is required for all commands");
                    }
                    _rootCommand = registration.Command;
                }
                return _rootCommand;
            }
            set
            {
                _rootCommand = NamesValidator.GetValidCommandName(value);
            }
        }

        /// <summary>
        /// Sets the help service used by all AbstractCommand instances for help formatting.
        /// Should be set during application startup, typically by the CommandController.
        /// TODO: make this non-static !!!
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
        /// Generates full help output for the command using the configured help service.
        /// </summary>
        /// <param name="parameters">Command parameters for context</param>
        /// <param name="env">Environment context</param>
        /// <returns>Formatted help string</returns>
        public virtual string Help(string[] parameters, IEnvironmentContext env)
        {
            if (_helpService == null)
            {
                throw new InvalidOperationException(
                    "HelpService has not been configured. Call AbstractCommand.SetHelpService() during application startup.");
            }

            return _helpService.BuildHelp(this, parameters, env);
        }

        /// <summary>
        /// Generates single line help summary for command listing.
        /// </summary>
        /// <param name="parameters">Command parameters for context</param>
        /// <returns>Single line formatted help string</returns>
        public virtual string OneLineHelp(string[] parameters)
        {
            if (_helpService == null)
            {
                var thisType = GetType();
                var baseCommand = Attribute.GetCustomAttribute(thisType, typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;
                if (baseCommand == null)
                {
                    throw new InvalidOperationException("CommandRegisterAttribute is required for all commands");
                }

                var isRoot = Attribute.GetCustomAttribute(thisType, typeof(CommandRootAttribute)) is CommandRootAttribute;
                var prefix = isRoot ? "-\t" : "";
                return $"{prefix}{baseCommand.Command,-12} {baseCommand.Description}";
            }

            var commandParameters = new CommandParameters();
            var commandDesc = commandParameters.CreatePackageDescription(GetType(), null!);
            return _helpService.BuildOneLineHelp(commandDesc);
        }

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            var processedParameters = ProcessParameters(io);

            if (io.HasPipedInput)
            {
                OnStartPipe(processedParameters, environment);

                await foreach (var pipedResult in io.ReadInputPipeChunks())
                {
                    if (!pipedResult.IsSuccess)
                    {
                        yield return pipedResult;
                        continue;
                    }

                    if (string.IsNullOrEmpty(pipedResult.Output)) continue;

                    var chunkResult = HandlePipedChunk(pipedResult, processedParameters, environment);
                    yield return chunkResult;
                }

                OnEndPipe(processedParameters, environment);
            }
            else
            {
                var parameterArray = io.Parameters ?? Array.Empty<string>();
                var isHelp = _helpService?.IsHelpRequest(parameterArray) ?? false;
                if (isHelp)
                {
                    yield return CommandResult<string>.Success(Help(parameterArray, environment), ResultFormat.General);
                }
                else
                {
                    var executionResult = HandleExecution(processedParameters, environment);
                    yield return executionResult;
                }
            }
        }

        /// <summary>
        /// Processes command parameters using attributes into a typed dictionary.
        /// </summary>
        /// <param name="parameters">Raw command parameters</param>
        /// <param name="hasPipedInput">Whether command is receiving piped input</param>
        /// <returns>Dictionary of processed parameter values</returns>
        public Dictionary<string, IParameterValue> ProcessParameters(IIoContext io)
        {
            var hasPipedInput = io.HasPipedInput;
            if (io.Parameters.Length == 0)
            {
                return new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            }

            var commandParameters = new CommandParameters();
            var processedParameters = commandParameters.ProcessParameters(
                io.Parameters,
                GetOrderedParameters(hasPipedInput),
                GetFlagParameters(),
                GetNamedParameters(hasPipedInput),
                GetSuffixParameters(hasPipedInput));

            // Inject parameter values into public instance fields
            SetParameterFields(processedParameters, io);

            return processedParameters;
        }

        /// <summary>
        /// Sets public instance fields on this command from processed parameters.
        /// Matches parameter names to field names case-insensitively.
        /// </summary>
        /// <param name="processedParameters">Dictionary of processed parameter values</param>
        private void SetParameterFields(Dictionary<string, IParameterValue> processedParameters, IIoContext io)
        {
            if (processedParameters == null || processedParameters.Count == 0)
                return;

            
            // Get public instance fields from the command type
            var commandType = GetType();
            var publicFields = commandType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            if (publicFields.Length == 0)
                return;

            // Match and set field values from parameters (case-insensitive)
            foreach (var field in publicFields)
            {
                if (processedParameters.TryGetValue(field.Name, out var parameterValue) && 
                    parameterValue != null && 
                    parameterValue.IsValid)
                {
                    try
                    {
                        // Use reflection to call the generic GetValue<T> method with the field's type
                        var getValue = typeof(IParameterValue).GetMethod(nameof(IParameterValue.TryGetValue));
                        if (getValue != null)
                        {
                            var genericGetValue = getValue.MakeGenericMethod(field.FieldType);
                            var parameters = new object?[] { null };
                            var success = (bool)genericGetValue.Invoke(parameterValue, parameters)!;
                            
                            if (success && parameters[0] != null)
                            {
                                field.SetValue(this, parameters[0]);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        var fieldName = (string.IsNullOrEmpty(field?.Name)) ? "UNKNOWN" : field.Name;
                        io.AddTraceMessage($"Failed to set field '{fieldName}' from parameter.").Wait();
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves ordered parameter attributes from the command type.
        /// </summary>
        /// <param name="hasPipedInput">If true, excludes parameters marked with UsePipe</param>
        /// <returns>Array of ordered parameter attributes</returns>
        protected CommandParameterOrderedAttribute[] GetOrderedParameters(bool hasPipedInput)
        {
            var thisType = GetType();
            var attributes = Attribute.GetCustomAttributes(thisType, typeof(CommandParameterOrderedAttribute)) as CommandParameterOrderedAttribute[] ?? Array.Empty<CommandParameterOrderedAttribute>();

            // when there is piped input, exclude parameters that use pipe, because the piped input will fill them
            return hasPipedInput 
                ? attributes.Where(x => !x.UsePipe).ToArray() 
                : attributes;
        }

        /// <summary>
        /// Retrieves named parameter attributes from the command type.
        /// </summary>
        /// <param name="hasPipedInput">If true, excludes parameters marked with UsePipe</param>
        /// <returns>Array of named parameter attributes</returns>
        protected CommandParameterNamedAttribute[] GetNamedParameters(bool hasPipedInput)
        {
            var thisType = GetType();
            var attributes = Attribute.GetCustomAttributes(thisType, typeof(CommandParameterNamedAttribute)) as CommandParameterNamedAttribute[] ?? Array.Empty<CommandParameterNamedAttribute>();

            // when there is piped input, exclude parameters that use pipe, because the piped input will fill them
            return hasPipedInput 
                ? attributes.Where(x => !x.UsePipe).ToArray() 
                : attributes;
        }

        /// <summary>
        /// Retrieves flag parameter attributes from the command type.
        /// </summary>
        /// <returns>Array of flag attributes</returns>
        protected CommandFlagAttribute[] GetFlagParameters()
        {
            var thisType = GetType();
            return Attribute.GetCustomAttributes(thisType, typeof(CommandFlagAttribute)) as CommandFlagAttribute[] ?? Array.Empty<CommandFlagAttribute>();
        }

        /// <summary>
        /// Retrieves suffix parameter attributes from the command type.
        /// </summary>
        /// <param name="hasPipedInput">If true, excludes parameters marked with UsePipe</param>
        /// <returns>Array of suffix parameter attributes</returns>
        protected CommandParameterSuffixAttribute[] GetSuffixParameters(bool hasPipedInput)
        {
            var thisType = GetType();
            var attributes = Attribute.GetCustomAttributes(thisType, typeof(CommandParameterSuffixAttribute)) as CommandParameterSuffixAttribute[] ?? Array.Empty<CommandParameterSuffixAttribute>();

            // when there is piped input, exclude parameters that use pipe, because the piped input will fill them
            return hasPipedInput 
                ? attributes.Where(x => !x.UsePipe).ToArray() 
                : attributes;
        }
        /// <summary>
        /// Executes the operation using the specified parameters and environment context, and returns the result.
        /// </summary>
        /// <param name="parameters">A dictionary containing parameter names and their corresponding values to be used during execution. Cannot
        /// be null.</param>
        /// <param name="env">The environment context in which the operation is executed. Cannot be null.</param>
        /// <returns>An IResult containing the execution result with success state, output, and optional error details.</returns>
        public abstract IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env);
        /// <summary>
        /// Processes a chunk of piped input and returns the result after applying the specified parameters
        /// and environment context.
        /// </summary>
        /// <param name="pipedChunk">The input string representing the chunk of data to process. Cannot be null.</param>
        /// <param name="parameters">A dictionary of parameter names and their corresponding values to be used during processing. Cannot be null.</param>
        /// <param name="env">The environment context that provides additional information or services required for processing. Cannot be
        /// null.</param>
        /// <returns>An IResult containing the processed result of the input chunk with success state and output.</returns>
        public abstract IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env);

        /// <summary>
        /// Invoked when a pipe operation is starting, allowing derived classes to perform custom initialization.
        /// </summary>
        /// <param name="processedParameters">Dictionary containing processed parameter values</param>
        /// <param name="environment">Environment context</param>
        protected virtual void OnStartPipe(Dictionary<string, IParameterValue> processedParameters, IEnvironmentContext environment)
        {
        }

        /// <summary>
        /// Invoked when pipe processing has completed, allowing for custom post-processing or cleanup.
        /// </summary>
        /// <param name="processedParameters">Dictionary containing processed parameter values</param>
        /// <param name="environment">Environment context</param>
        protected virtual void OnEndPipe(Dictionary<string, IParameterValue> processedParameters, IEnvironmentContext environment)
        {
        }

        public virtual Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Retrieves a list of all parameters associated with the command, including ordered, flag, named, and suffix
        /// parameters. You must override this method if you are not using the standard attribute-based parameter definition.
        /// </summary>
        /// <returns>A list of <see cref="ICommandParameter"/> objects representing all parameters for the command.</returns>
        public virtual List<ICommandParameter> GetParameters()
        {
            var parameters = new List<ICommandParameter>();
            parameters.AddRange(GetOrderedParameters(false).ToList<ICommandParameter>());
            parameters.AddRange(GetFlagParameters().ToList<ICommandParameter>());
            parameters.AddRange(GetNamedParameters(false).ToList<ICommandParameter>());
            parameters.AddRange(GetSuffixParameters(false).ToList<ICommandParameter>());
            return parameters;
        }
    }
}
