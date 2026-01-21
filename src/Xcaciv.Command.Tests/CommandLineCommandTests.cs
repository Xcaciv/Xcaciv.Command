using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command;
using Xcaciv.Command.Extensions.Commandline;
using Xcaciv.Command.Interface;
using Xunit;
using SystemCommand = System.CommandLine.Command;

namespace Xcaciv.Command.Tests
{
    public class CommandLineCommandTests
    {
        [Fact]
        public async Task Main_ReturnsFailureWhenNotInitialized()
        {
            var adapter = new CommandLineCommand<SystemCommand>();
            var ioContext = new MemoryIoContext(parameters: Array.Empty<string>());
            var environment = new EnvironmentContext();
            var results = new List<IResult<string>>();

            await foreach (var result in adapter.Main(ioContext, environment))
            {
                results.Add(result);
            }

            var resultEntry = Assert.Single(results);
            Assert.False(resultEntry.IsSuccess);
            Assert.Contains("SetCommand", resultEntry.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Main_ExecutesActionAndReturnsOutput()
        {
            var systemCommand = new SystemCommand("echo", "writes content");
            systemCommand.SetAction(parseResult => Console.Out.Write("wrapped-output"));

            var adapter = new CommandLineCommand<SystemCommand>();
            adapter.SetCommand(systemCommand);

            var ioContext = new MemoryIoContext(parameters: Array.Empty<string>());
            var environment = new EnvironmentContext();
            var results = new List<IResult<string>>();

            await foreach (var result in adapter.Main(ioContext, environment))
            {
                results.Add(result);
            }

            var resultEntry = Assert.Single(results);
            Assert.True(resultEntry.IsSuccess);
            Assert.Equal("wrapped-output", resultEntry.Output);
        }

        [Fact]
        public async Task Main_UsesPipedInputAsConsoleIn()
        {
            var systemCommand = new SystemCommand("read", "reads piped input");
            systemCommand.SetAction(parseResult =>
            {
                var piped = Console.In.ReadToEnd();
                Console.Out.Write(piped);
            });

            var adapter = new CommandLineCommand<SystemCommand>();
            adapter.SetCommand(systemCommand);

            var channel = Channel.CreateUnbounded<IResult<string>>();
            await channel.Writer.WriteAsync(CommandResult<string>.Success("pipe-value"));
            channel.Writer.Complete();

            var ioContext = new MemoryIoContext(parameters: Array.Empty<string>());
            ioContext.SetInputPipe(channel.Reader);

            var environment = new EnvironmentContext();
            var results = new List<IResult<string>>();

            await foreach (var result in adapter.Main(ioContext, environment))
            {
                results.Add(result);
            }

            var resultEntry = Assert.Single(results);
            Assert.True(resultEntry.IsSuccess, $"Expected success but got: {resultEntry.ErrorMessage}");
            Assert.Equal("pipe-value" + Environment.NewLine, resultEntry.Output);
        }

        [Fact]
        public async Task Help_UsesSystemCommandHelp()
        {
            var systemCommand = new SystemCommand("greet", "writes greetings");
            systemCommand.SetAction(parseResult => Console.Out.Write("greetings"));

            var adapter = new CommandLineCommand<SystemCommand>();
            adapter.SetCommand(systemCommand);

            var ioContext = new MemoryIoContext(parameters: new[] { "--help" });
            var environment = new EnvironmentContext();
            var results = new List<IResult<string>>();

            await foreach (var result in adapter.Main(ioContext, environment))
            {
                results.Add(result);
            }

            var output = string.Join(Environment.NewLine, results.Where(r => r.IsSuccess).Select(r => r.Output));
            Assert.Contains("greet", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("writes greetings", output, StringComparison.OrdinalIgnoreCase);
        }
    }
}
