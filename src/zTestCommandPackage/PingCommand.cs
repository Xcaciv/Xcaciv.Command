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
    [CommandRegister("PING", "PING")]
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

        public string Help(string[] parameters, IEnvironmentContext evn)
        {
            return $"[{BaseCommand}] ({FriendlyName}) - test command to output each parameter as a chunk";
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
            else if (io.Parameters.Length > 0 && io.Parameters[0].Equals("--HELP", StringComparison.CurrentCultureIgnoreCase))
            {
                yield return CommandResult<string>.Success(this.Help(io.Parameters, statusContext));
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

        public string OneLineHelp(string[] paramseters)
        {
            return $"{BaseCommand} - {FriendlyName}";
        }
    }
}
