# Quick Start Guide

Get up and running with Xcaciv.Command in 5 minutes.

## 1. Install the Framework

Add NuGet packages to your project:

```bash
dotnet add package Xcaciv.Command
dotnet add package Xcaciv.Command.Core
dotnet add package Xcaciv.Command.Interface
```

## 2. Create Your First Command

Create a file `GreetCommand.cs`:

```csharp
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

[CommandRegister("GREET", "Greet someone")]
[CommandParameterOrdered(0, "name", "Person's name")]
public class GreetCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env)
    {
        var name = parameters["name"].GetValue<string>();
        return CommandResult<string>.Success($"Hello, {name}!");
    }

    public override IResult<string> HandlePipedChunk(
        IResult<string> pipedChunk,
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env)
    {
        if (!pipedChunk.IsSuccess)
        {
            return pipedChunk;
        }

        var input = pipedChunk.Output ?? string.Empty;
        return CommandResult<string>.Success($"Hello, {input}!");
    }
}
```

## 3. Create a Console Application

Create `Program.cs`:

```csharp
using Xcaciv.Command;
using Xcaciv.Command.Interface;

var controller = new CommandController();
controller.RegisterBuiltInCommands();
controller.AddCommand("Samples", typeof(GreetCommand));

var ioContext = new MemoryIoContext();
var env = new ControllerEnvironmentContext();

while (true)
{
    var commandLine = await ioContext.PromptForCommand("xcaciv> ");
    if (string.IsNullOrWhiteSpace(commandLine)) break;

    await controller.Run(commandLine, ioContext, env);
}
```

## 4. Try It

Run and execute:

```shell
xcaciv> GREET Alice
Hello, Alice!

xcaciv> SAY World | GREET
Hello, World!

xcaciv> HELP GREET
```

## 5. Load Plugins

Create a plugin directory structure:

```shell
/plugins/
  MyPlugin/
    bin/
      MyPlugin.dll
      MyPlugin.deps.json
```

Update your application:

```csharp
var controller = new CommandController();
controller.RegisterBuiltInCommands();
controller.AddPackageDirectory("/plugins");
controller.LoadCommands();
```

## 6. Create a Pipeline

Commands can be chained with `|`:

```csharp
await controller.Run("SAY Hello | REGIF ^Hello", ioContext, env);
```

## Built-in Commands

| Command | Usage | Description |
|---------|-------|-------------|
| SAY | SAY text | Output text |
| SET | SET NAME value | Set environment variable |
| ENV | ENV | Show environment variables |
| REGIF | PATTERN | Filter by regex |
| HELP | HELP [command] | Show help |

## Common Patterns

### Add Parameters to Your Command

```csharp
[CommandRegister("SEND", "Send a message")]
[CommandParameterOrdered(0, "to", "Recipient")]
[CommandParameterOrdered(1, "message", "Message text")]
[CommandParameterNamed("subject", "Email subject")]
[CommandFlag("urgent", "Mark as urgent")]
public class SendCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env)
    {
        var to = parameters["to"].GetValue<string>();
        var message = parameters["message"].GetValue<string>();
        var subject = parameters.TryGetValue("subject", out var s) && s.IsValid ? s.GetValue<string>() : string.Empty;
        var urgent = parameters.TryGetValue("urgent", out var u) && u.IsValid && u.GetValue<bool>();

        var priority = urgent ? "URGENT" : "normal";
        return CommandResult<string>.Success($"[{priority}] To: {to}, Subject: {subject}, Message: {message}");
    }

    public override IResult<string> HandlePipedChunk(
        IResult<string> pipedChunk,
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env)
    {
        return pipedChunk;
    }
}
```

Usage:

```shell
xcaciv> SEND alice "Please review" --subject "Code Review" --urgent
[URGENT] To: alice, Subject: Code Review, Message: Please review
```

### Handle Piped Input

```csharp
[CommandRegister("REVERSE", "Reverse text")]
public class ReverseCommand : AbstractCommand
{
    public override async IAsyncEnumerable<IResult<string>> Main(
        IIoContext ioContext,
        IEnvironmentContext env)
    {
        if (!ioContext.HasPipedInput)
        {
            yield break;
        }

        await foreach (var chunk in ioContext.ReadInputPipeChunks())
        {
            if (!chunk.IsSuccess)
            {
                yield return chunk;
                continue;
            }

            var text = chunk.Output ?? string.Empty;
            var reversed = new string(text.Reverse().ToArray());
            yield return CommandResult<string>.Success(reversed);
        }
    }

    public override IResult<string> HandleExecution(
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env) => CommandResult<string>.Success(string.Empty);

    public override IResult<string> HandlePipedChunk(
        IResult<string> pipedChunk,
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env) => pipedChunk;
}
```

Usage:

```shell
xcaciv> SAY hello | REVERSE
olleh
```

### Access Environment Variables

```csharp
public override string HandleExecution(string[] parameters, IEnvironmentContext env)
{
    var appName = env.GetVariable("APP_NAME") ?? "MyApp";
    var userName = env.GetVariable("USER") ?? "unknown";
    
    return $"User {userName} using {appName}";
}
```

### Modify Environment

```csharp
[CommandRegister("SETVAR", "Set variable")]
public class SetVarCommand : AbstractCommand
{
    [CommandParameterOrdered("name", "Variable name")]
    public string Name { get; set; }

    [CommandParameterOrdered("value", "Variable value")]
    public string Value { get; set; }

    public override string HandleExecution(string[] parameters, IEnvironmentContext env)
    {
        env.SetVariable(Name, Value);
        return $"Set {Name}={Value}";
    }
}
```

Register with modification flag:

```csharp
controller.AddCommand("MyApp", typeof(SetVarCommand), modifiesEnvironment: true);
```

## Debug Your Commands

### Use MemoryIoContext for Testing

```csharp
[Fact]
public async Task GreetCommand_ReturnsGreeting()
{
    var controller = new CommandController();
    controller.AddCommand("test", typeof(GreetCommand));

    var io = new MemoryIoContext(new[] { "Alice" });
    var env = new EnvironmentContext();

    await controller.Run("GREET Alice", io, env);

    string output = io.GetOutput();
    Assert.Equal("Hello, Alice!", output);
}
```

### Enable Audit Logging

```csharp
public class ConsoleAuditLogger : IAuditLogger
{
    public async Task LogAsync(string entry)
    {
        Console.WriteLine($"[AUDIT] {entry}");
        await Task.CompletedTask;
    }
}

controller.SetAuditLogger(new ConsoleAuditLogger());
```

## Next Steps

- [Create a Command](getting-started-create-command.md) - Detailed guide
- [Build a Plugin](getting-started-plugins.md) - Package commands for distribution
- [Use Pipelines](getting-started-pipelines.md) - Chain commands together
- [API Reference](api-core.md) - Complete API documentation

## Troubleshooting

### Command Not Found

**Problem:** "Command 'MYCOMMAND' is not registered"

**Solution:** Ensure command class has `CommandRegisterAttribute` and is added to controller

```csharp
[CommandRegister("MYCOMMAND", "description")] // ? Don't forget this
public class MyCommand : AbstractCommand { }

controller.AddCommand("pkg", typeof(MyCommand)); // ? Register it
```

### Plugin Not Loading

**Problem:** "No plugins found" or "No plugin files found"

**Solution:** Check directory structure

```shell
/plugins/
  MyPlugin/
    bin/              ? LoadCommands() searches here by default
      MyPlugin.dll
      MyPlugin.deps.json
```

### Parameter Not Working

**Problem:** Parameter not being parsed correctly

**Solution:** Ensure attributes are in correct order

```csharp
[CommandRegister("CMD", "desc")]
public class MyCommand : AbstractCommand
{
    [CommandParameterOrdered("first", "desc")]  // 1. Ordered first
    public string First { get; set; }

    [CommandFlag("flag", "desc")]               // 2. Flags second
    public bool Flag { get; set; }

    [CommandParameterNamed("key", "desc")]      // 3. Named third
    public string Key { get; set; }

    [CommandParameterSuffix("rest", "desc")]    // 4. Suffix last
    public string[] Rest { get; set; }
}
```

## Resources

- [API Reference](api-interfaces.md)
- [Architecture](architecture.md)
- [GitHub Repository](https://github.com/xcaciv/Xcaciv.Command)
