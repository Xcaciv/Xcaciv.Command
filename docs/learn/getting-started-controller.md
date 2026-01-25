# Configure the CommandController

Learn how to set up and configure the `CommandController` to execute commands.

## What is CommandController?

`CommandController` is the central orchestrator that:

- Manages command registration and discovery
- Routes command execution
- Supports plugin loading
- Handles pipelines and I/O
- Manages audit logging

## Basic Setup

### Create and Configure

```csharp
using Xcaciv.Command;
using Xcaciv.Command.Interface;

var controller = new CommandController();
controller.RegisterBuiltInCommands();

var ioContext = new MemoryIoContext();
var environmentContext = new ControllerEnvironmentContext();

await controller.Run("SAY Hello World", ioContext, environmentContext);
```

### Output

```
Hello World
```

## Constructor Overloads

### Default Constructor

```csharp
var controller = new CommandController();
```

Creates a controller with default components.

### With Custom Crawler

```csharp
var crawler = new Crawler();
var controller = new CommandController(crawler);
```

Use when you need to customize plugin discovery.

### With Restricted Directory

```csharp
var controller = new CommandController(crawler, "/opt/plugins");
```

Restricts plugin loading to a specific directory for security.

### With Custom Verified Directories

```csharp
var directories = new VerifiedSourceDirectories(fileSystem);
var controller = new CommandController(directories);
```

Use when implementing custom directory verification logic.

### Full Dependency Injection

```csharp
var registry = new CommandRegistry();
var loader = new CommandLoader(crawler, directories);
var pipeline = new PipelineExecutor();
var executor = new CommandExecutor(registry, new CommandFactory(serviceProvider), new HelpService());

var controller = new CommandController(
    registry,
    loader,
    pipeline,
    executor,
    new CommandFactory(serviceProvider),
    serviceProvider: null);
```

All dependencies are injectable for testing and customization.

## Adding Commands

### Built-in Commands

```csharp
controller.RegisterBuiltInCommands();
```

Registers these commands:

- `SAY` - Output text
- `SET` - Set environment variable
- `ENV` - Display environment variables
- `REGIF` - Regular expression filtering
- `HELP` - Display help (via `HELP` or help flags like `--HELP`)

### From a Type

```csharp
[CommandRegister("MYCMD", "My custom command")]
[CommandParameterOrdered(0, "value", "A value")]
public class MyCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env) => CommandResult<string>.Success("Output");

    public override IResult<string> HandlePipedChunk(
        IResult<string> pipedChunk,
        Dictionary<string, IParameterValue> parameters,
        IEnvironmentContext env) => pipedChunk;
}

controller.AddCommand("MyPackage", typeof(MyCommand), modifiesEnvironment: false);
```

### From an Instance

```csharp
controller.AddCommand("MyPackage", new MyCommand(), modifiesEnvironment: false);
```

### From Package Directory

```csharp
controller.AddPackageDirectory("/opt/plugins");
controller.LoadCommands(); // searches bin/ by default
controller.LoadCommands("lib"); // custom subdirectory
```

## Running Commands

### Basic Execution

```csharp
var ioContext = new MemoryIoContext();
var env = new ControllerEnvironmentContext();
await controller.Run("MYCOMMAND arg1 arg2", ioContext, env);
```

### With Pipelining

```csharp
await controller.Run("SAY line1 | REGIF ^l", ioContext, env);
```

### Cancellation

```csharp
var cts = new CancellationTokenSource();
await controller.Run("SAY Hello", ioContext, env, cts.Token);
```

## Help System

Help is served via the `HELP` command or help flags (`--HELP`, `-?`, `/?`).

```csharp
await controller.Run("HELP SAY", ioContext, env);
```

## Audit Logging

Set an `IAuditLogger` to capture command execution and environment changes:

```csharp
controller.AuditLogger = new StructuredAuditLogger();
await controller.Run("SAY hi", ioContext, env);
```

## Environment Propagation

Only commands registered with `modifiesEnvironment: true` propagate changes back to the controller environment after execution. Pipelines scope environments per stage and merge back only when allowed.
