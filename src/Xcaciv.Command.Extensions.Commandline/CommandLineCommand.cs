using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Parameters;
using SystemCommand = System.CommandLine.Command;

namespace Xcaciv.Command.Extensions.Commandline
{
    /// <summary>
    /// Wraps System.CommandLine.Command instances to work within the Xcaciv.Command pipeline.
    /// Console redirection is synchronized across concurrent instances to prevent interference.
    /// </summary>
    public class CommandLineCommand<T> : ICommandDelegate where T : SystemCommand
    {
        private static readonly SemaphoreSlim ConsoleRedirectionSemaphore = new(1, 1);
        private T? command;

        protected T? WrappedCommand => command;

        public string Command => command?.Name ?? string.Empty;

        public string RootCommand => string.Empty;

        public virtual void SetCommand(T commandToWrap)
        {
            command = commandToWrap ?? throw new ArgumentNullException(nameof(commandToWrap));
            EnsureHelpOption(command);
        }

        public virtual async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            if (command == null)
            {
                yield return CommandResult<string>.Failure("Command has not been initialized. Call SetCommand before execution.");
                yield break;
            }

            var pipedInput = await CollectPipedInput(ioContext).ConfigureAwait(false);

            var standardOutWriter = new StringWriter();
            var standardErrorWriter = new StringWriter();
            StringReader? standardInReader = null;

            var originalOut = Console.Out;
            var originalError = Console.Error;
            var originalIn = Console.In;

            try
            {
                Console.SetOut(standardOutWriter);
                Console.SetError(standardErrorWriter);

                if (!string.IsNullOrEmpty(pipedInput))
                {
                    standardInReader = new StringReader(pipedInput);
                    Console.SetIn(standardInReader);
                }

                // ioContext.Parameters are pre-tokenized strings from Xcaciv.Command framework.
                // System.CommandLine.Parse expects command-line arguments as they would appear
                // on the command line. The framework tokenizes the input, so values with spaces
                // are already separated into individual array elements, making them compatible
                // with System.CommandLine's parser expectations.
                var parseResult = command.Parse(ioContext.Parameters ?? Array.Empty<string>());
                var exitCode = await parseResult.InvokeAsync().ConfigureAwait(false);

                var output = standardOutWriter.ToString();
                var errorOutput = standardErrorWriter.ToString();

                if (exitCode == 0)
                {
                    yield return CommandResult<string>.Success(output);
                }
                else
                {
                    var failureMessage = string.IsNullOrWhiteSpace(errorOutput)
                        ? $"Command '{command.Name}' exited with code {exitCode}."
                        : errorOutput;
                    yield return CommandResult<string>.Failure(failureMessage);
                }
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                Console.SetIn(originalIn);
                standardOutWriter.Dispose();
                standardErrorWriter.Dispose();
                standardInReader?.Dispose();
            }
        }

        public virtual ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public virtual Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual List<ICommandParameter> GetParameters()
        {
            return new List<ICommandParameter>();
        }

        private static async Task<string> CollectPipedInput(IIoContext ioContext)
        {
            if (!ioContext.HasPipedInput)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            await foreach (var chunk in ioContext.ReadInputPipeChunks().ConfigureAwait(false))
            {
                if (chunk == null || string.IsNullOrEmpty(chunk.Output))
                {
                    continue;
                }

                builder.Append(chunk.Output);
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void EnsureHelpOption(SystemCommand commandToWrap)
        {
            var hasHelp = commandToWrap.Options.Any(option => option is HelpOption || option.Aliases.Any(alias => alias.Equals("--help", StringComparison.OrdinalIgnoreCase)));
            if (!hasHelp)
            {
                commandToWrap.Add(new HelpOption());
            }
        }
    }
}
