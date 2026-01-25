# API Reference: Interfaces

Complete reference for public interfaces in the Xcaciv.Command framework.

## ICommandController

Central interface for command execution and plugin management.

```csharp
public interface ICommandController
{
    // Built-in commands
    void RegisterBuiltInCommands();

    // Plugin directory management
    void AddPackageDirectory(string directory);
    void LoadCommands(string subDirectory = "bin");

    // Command execution
    Task Run(string commandLine, IIoContext output, IControllerEnvironmentContext env);
    Task Run(string commandLine, IIoContext output, IEnvironmentContext env);
    Task Run(string commandLine, IIoContext output, IEnvironmentContext env, CancellationToken cancellationToken);

    // Manual command registration
    void AddCommand(ICommandDescription command);
    void AddCommand(string packageKey, Type commandType, bool modifiesEnvironment = false);
    void AddCommand(string packageKey, ICommandDelegate command, bool modifiesEnvironment = false);
}
```

### Methods

#### RegisterBuiltInCommands()

Registers built-in commands:

- `SAY` - Output text
- `SET` - Set environment variable
- `ENV` - Display environment variables
- `REGIF` - Regular expression filter
- `HELP` - Display help (triggered via `HELP` command or help flags like `--HELP`)

#### AddPackageDirectory(string directory)

Adds a directory where the framework searches for plugin packages.

**Parameters:**

- `directory` (`string`): Path to plugin directory

#### LoadCommands(string subDirectory = "bin")

Discovers and loads commands from plugin directories.

**Parameters:**

- `subDirectory` (`string`, optional): Subdirectory within each plugin to search (default: "bin")

#### Run(...)

Executes a command line with optional cancellation and environment scoping.

**Example:**

```csharp
await controller.Run("SAY Hello | REGIF ^Hello", ioContext, env);
```

#### AddCommand overloads

Register commands via description, type, or instance; `modifiesEnvironment` controls whether environment changes propagate back to the controller context.

---

## ICommandDelegate

Contract for executable commands.

```csharp
public interface ICommandDelegate : IAsyncDisposable
{
    string Command { get; }
    string RootCommand { get; }
    IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env);
    Dictionary<string, string> GetDefaultEnvironment();
    List<ICommandParameter> GetParameters();
}
```

### Methods

- **Main(...)**: Primary execution; yield `IResult<string>` chunks. Pipelined input is available via `ioContext.ReadInputPipeChunks()`.
- **GetDefaultEnvironment()**: Default environment variables (names are prefixed when stored globally).
- **GetParameters()**: Parameter definitions; `AbstractCommand` can auto-populate from parameter attributes.

---

## IIoContext

Manages command input/output and parameter access.

```csharp
public interface IIoContext : ICommandContext<IIoContext>, IAsyncDisposable
{
    bool HasPipedInput { get; }
    string[] Parameters { get; }

    void SetInputPipe(ChannelReader<IResult<string>> reader);
    IAsyncEnumerable<IResult<string>> ReadInputPipeChunks();
    Task<string> PromptForCommand(string prompt);
    void SetOutputPipe(ChannelWriter<IResult<string>> writer);
    Task OutputChunk(IResult<string> message);
    Task SetStatusMessage(string message);
    Task AddTraceMessage(string message);
    Task<int> SetProgress(int total, int step);
    Task Complete(string? message);
    Task SetParameters(string[] parameters);
    void SetOutputEncoder(IOutputEncoder encoder);
    int? PipelineStage { get; }
    int? PipelineTotalStages { get; }
    void SetPipelineStage(int stage, int totalStages);
}
```

- **HasPipedInput**: True when the context is receiving piped input.
- **SetInputPipe/ReadInputPipeChunks**: Channel-based pipeline support using `IResult<string>` to preserve success/error metadata.
- **SetOutputPipe/OutputChunk**: Send output downstream as `IResult<string>` chunks.
- **Trace/Status/Progress**: Structured diagnostics and progress reporting.
- **SetParameters**: Internal use when the controller rewrites parameters for sub-commands.

---

## IEnvironmentContext and IControllerEnvironmentContext

Environment scopes for commands and controllers. Commands receive a child environment; only commands flagged with `ModifiesEnvironment` propagate changes back to the controller environment.

- `IEnvironmentContext`: Command-level scope.
- `IControllerEnvironmentContext`: Controller-level scope that tracks per-command environments and supports propagation.

---

## ICommandParameter

Represents a command parameter definition (ordered, named, flag, or suffix). Attribute-based commands typically use:

- `CommandParameterOrderedAttribute`
- `CommandParameterNamedAttribute`
- `CommandFlagAttribute`
- `CommandParameterSuffixAttribute`

`AbstractCommand.GetParameters()` aggregates these definitions unless overridden.
