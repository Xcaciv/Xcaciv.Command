# Xcaciv.Command

Excessively modular, async pipeable, text command framework.

## Quick Start

```csharp
using Xcaciv.Command;
using Xcaciv.Command.Interface;

var controller = new CommandController();
controller.RegisterBuiltInCommands();

var env = new ControllerEnvironmentContext();
var ioContext = new MemoryIoContext();

await controller.Run("Say Hello to my little friend", ioContext, env);
// outputs: Hello to my little friend
```

## Latest Changes

- Pipeline execution is fully async and stream-friendly; commands can yield intermediate chunks to downstream consumers.
- Command loading supports verified package directories via `AddPackageDirectory` + `LoadCommands`, confining each plugin to its path-restricted security policy.
- Examples and templates use `RegisterBuiltInCommands` and the v3.2.3+ `HandlePipedChunk(IResult<string>)` signature.

## Loading External Commands

```csharp
var controller = new CommandController(new Crawler(), restrictedDirectory);
controller.AddPackageDirectory("path/to/plugins");
controller.LoadCommands();

await controller.Run("your-command args", ioContext, env);
```

## Creating Commands

Commands are .NET class libraries that implement `ICommandDelegate` (usually via `AbstractCommand`) and use attributes for parameters:

```csharp
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

Register the command:

```csharp
controller.AddCommand("MyPackage", typeof(GreetCommand));
```

## Security

This framework uses **Xcaciv.Loader 2.1.2** with instance-based security policies:

- Each plugin is loaded with directory-based path restrictions
- Default security policy prevents access outside plugin directories
- Security violations are logged and handled gracefully
- Per-instance configuration; each `AssemblyContext` has independent security

## Built-in Commands

- **SAY**: Output text to the context
- **SET**: Set environment variables
- **ENV**: Display environment variables
- **REGIF**: Conditional execution based on regex

## Pipeline Support

Chain commands with `|` for pipeline execution:

```csharp
await controller.Run("command1 arg1 | command2 | command3", ioContext, env);
```

## Dependencies

- Xcaciv.Loader 2.1.2 - Secure assembly loading
- Xcaciv.Command.Interface - Core interfaces
- Xcaciv.Command.Core - Base implementations
- Xcaciv.Command.FileLoader - Plugin discovery

## Project Links

- GitHub: https://github.com/Xcaciv/Xcaciv.Command
- License: AGPL-3.0