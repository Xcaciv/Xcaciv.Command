using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;

namespace zTestCommandPackage
{
    [CommandRegister("PING", "test command with parameters that outputs like echo")]
    [CommandParameterOrdered("echo_word", "ECHO")]
    [CommandParameterNamed("optional", "option", AllowedValues = ["1", "2"])]
    public class PingCommand : ICommandDelegate
    {
        public string Command => "PING";
        public string RootCommand => string.Empty;
        public string BaseCommand { get; protected set; } = "ECHO";

        public string FriendlyName { get; protected set; } = "echo";

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>();
        }

        public List<ICommandParameter> GetParameters()
        {
            return new List<ICommandParameter>();
        }

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext statusContext)
        {
            await io.AddTraceMessage($"{this.BaseCommand} test start");
            if (io.HasPipedInput)
            {
                await foreach (var pipedResult in io.ReadInputPipeChunks())
                {
                    if (pipedResult.IsSuccess && pipedResult.Output != null)
                    {
                        yield return CommandResult<string>.Success(this.FormatEcho(pipedResult.Output));
                    }
                    else
                    {
                        yield return pipedResult;
                    }
                }
            }
            else
            {
                foreach (var parameterValue in io.Parameters)
                {
                    yield return CommandResult<string>.Success(this.FormatEcho(parameterValue));
                }
            }
            await io.AddTraceMessage($"{this.BaseCommand} test end");
        }

        public virtual string FormatEcho(string text)
        {
            return $"{text}";
        }

        public ValueTask DisposeAsync()
        {
            // nothing to dispose
            return ValueTask.CompletedTask;
        }

    }
}
