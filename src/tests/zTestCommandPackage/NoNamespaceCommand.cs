using System.Collections.Generic;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;

// Deliberately declared without a namespace. A plugin command like this used to fail to
// load with "Type '.NoNamespaceCommand' was not found in assembly 'zTestCommandPackage, ...'".
[CommandRegister("NONS", "test command declared without a namespace")]
public class NoNamespaceCommand : ICommandDelegate
{
    public string Command => "NONS";
    public string RootCommand => string.Empty;
    public string BaseCommand { get; protected set; } = "NONS";

    public string FriendlyName { get; protected set; } = "nons";

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
        foreach (var parameterValue in io.Parameters)
        {
            yield return CommandResult<string>.Success(parameterValue);
        }
        await io.AddTraceMessage($"{this.BaseCommand} test end");
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
