using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests.TestImplementations
{
    /// <summary>
    /// Direct implementation of ICommandDelegate for tests that need full control
    /// without using AbstractCommand's parameter processing logic.
    /// </summary>
    public class TestCommandDelegate : ICommandDelegate
    {
        private readonly Func<IIoContext, IEnvironmentContext, IAsyncEnumerable<IResult<string>>>? _mainFunc;
        private readonly Func<string[], IEnvironmentContext, string>? _helpFunc;
        private readonly Func<string[], string>? _oneLineHelpFunc;
        private readonly Dictionary<string, string> _defaultEnvironment;

        public string Command { get; set; } = "TEST";

        public string RootCommand { get; set; } = string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(_defaultEnvironment, StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public TestCommandDelegate(
            Func<IIoContext, IEnvironmentContext, IAsyncEnumerable<IResult<string>>>? mainFunc = null,
            Func<string[], IEnvironmentContext, string>? helpFunc = null,
            Func<string[], string>? oneLineHelpFunc = null,
            Dictionary<string, string>? defaultEnvironment = null)
        {
            _mainFunc = mainFunc;
            _helpFunc = helpFunc;
            _oneLineHelpFunc = oneLineHelpFunc;
            _defaultEnvironment = defaultEnvironment ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            if (_mainFunc != null)
            {
                await foreach (var result in _mainFunc(io, environment))
                {
                    yield return result;
                }
            }
            else
            {
                yield return CommandResult<string>.Success("TestCommandDelegate executed");
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Simple echo command delegate for testing pipelines.
    /// </summary>
    public class EchoCommandDelegate : ICommandDelegate
    {
        private readonly string _prefix;
        private readonly Dictionary<string, string> _defaultEnvironment;

        public EchoCommandDelegate(string prefix = "", Dictionary<string, string>? defaultEnvironment = null)
        {
            _prefix = prefix;
            _defaultEnvironment = defaultEnvironment ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Command => "ECHO";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(_defaultEnvironment, StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            if (io.HasPipedInput)
            {
                await foreach (var pipedResult in io.ReadInputPipeChunks())
                {
                    // Propagate failures from upstream commands
                    if (!pipedResult.IsSuccess)
                    {
                        yield return pipedResult;
                        continue;
                    }
                    
                    if (pipedResult.Output != null)
                    {
                        yield return CommandResult<string>.Success(_prefix + pipedResult.Output);
                    }
                }
            }
            else if (io.Parameters != null && io.Parameters.Length > 0)
            {
                foreach (var param in io.Parameters)
                {
                    yield return CommandResult<string>.Success(_prefix + param);
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
