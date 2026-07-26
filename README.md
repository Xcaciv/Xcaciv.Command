# Xcaciv.Command: Excessive Command Framework

[![Securability](https://localhost:3001/badge/15b3f9cf-ec24-455d-bb3b-d00427a61692.svg)](https://localhost:3001/r/Xcaciv/Xcaciv.Command)

Excessively modular, async pipeable, command framework with strongly-typed parameter support.

```csharp
using Xcaciv.Command;
using Xcaciv.Command.Interface;

var controller = new CommandController();
controller.RegisterBuiltInCommands();

var io = new MemoryIoContext();
var env = new ControllerEnvironmentContext();

await controller.Run("Say Hello to my little friend", io, env);
```

Commands are .NET class libraries that contain implementations of the `Xc.Command.ICommandDelegate` interface and are decorated with attributes that describe the command and its parameters. These attributes filter and validate input and drive auto-generated help.

**Authoring note:** When building a new command, start from the template and guidance in [COMMAND_TEMPLATE.md](COMMAND_TEMPLATE.md) to match the latest interfaces (`ICommandDelegate`/`AbstractCommand`), `OutputFormat`, and environment propagation rules.

## Getting Started

- Read the quickstart in [docs/learn/quickstart.md](docs/learn/quickstart.md) for a five-minute walkthrough.
- When building a new command, start from [COMMAND_TEMPLATE.md](COMMAND_TEMPLATE.md) to match the latest interfaces (`ICommandDelegate`/`AbstractCommand`), `OutputFormat`, and environment propagation rules.

## Features

- **Strongly-Typed Parameters**: Generic `IParameterValue<T>` with compile-time type safety and pre-validation
- **Type-Safe Parameter Access**: `GetValue<T>()` method prevents accessing unconverted strings
- **Rich Type Support**: String, numeric types, bool, Guid, DateTime, JsonElement with nullable variants
- **Enhanced Diagnostics**: Detailed type mismatch and validation error messages with parameter context
- **Async Pipeline Support**: Commands can be chained with `|` for threaded pipeline execution
- **Plugin System**: Dynamic command loading from external assemblies
- **Secure Plugin Loading**: Built on Xcaciv.Loader 2.1.2 with instance-based security policies
- **Auto-Generated Help**: Comprehensive help generation from command attributes
- **Sub-Commands**: Hierarchical command structure support
- **Built-in Commands**: `SAY`, `SET`, `ENV`, and `REGIF` included
- **Target Frameworks**: Defaults to .NET 10.0; opt into multi-targeting with `.NET 8.0` via `build.ps1 -UseNet08`

## Dependencies

- **Xcaciv.Loader 2.1.2**: Assembly loading with instance-based security configuration
- **System.IO.Abstractions 22.1.0**: File system abstraction for testability
- **Microsoft.Extensions.* 10.0.1**
- **System.CommandLine 2.0.1** (Command Extensions only)

## Security

This framework uses Xcaciv.Loader 2.1.2 instance-based security policies:

- **Default Security Policy**: Each plugin is restricted to its own directory
- **No Wildcard Access**: Replaces v1.x wildcard (`*`) restrictions with proper path-based security
- **Security Exception Handling**: Graceful handling of security violations with detailed logging
- **Per-Instance Configuration**: Each AssemblyContext has independent security settings
- **Type Safety**: Parameters validated and converted before command execution prevents injection attacks

For detailed security model, plugin development guidelines, and vulnerability reporting procedures, see `SECURITY.md`.

### Audit Logging

Command execution and environment changes can be logged via `IAuditLogger`:

```csharp
var controller = new CommandController();
controller.RegisterBuiltInCommands();
var env = new ControllerEnvironmentContext();

controller.AuditLogger = new YourAuditLoggerImplementation();
await controller.Run("say hello", ioContext, env);
```

Logs include:

- Command execution with parameters, duration, and success status
- Environment variable changes with old/new values
- Timestamps in UTC for consistency

See `SECURITY.md` for secure audit logging patterns.

## Roadmap

- [X] Threaded pipelining
- [X] Internal commands `SAY` `SET`, `ENV` and `REGIF`
- [X] Sub-command structure
- [X] Auto generated help
- [X] Migrate to Xcaciv.Loader 2.1.2 with instance-based security
- [X] Type-safe parameter system with generics

## Version History

### 3.3.0 (Current)

- **Version bump:** All packages aligned to **3.3.0** (Command, Core, Interface, FileLoader, DependencyInjection, Extensions.Commandline)
- **Documentation refresh:** Quickstart, API references, and examples updated to use `RegisterBuiltInCommands` and the v3.2.3+ `HandlePipedChunk(IResult<string>)` signature
- **Defaults:** .NET 10 targeting with optional `.NET 8` multi-targeting via `UseNet08`

### 3.2.4

- **Version bump:** Packages aligned to **3.2.4**
- **API cleanup:** Removed duplicate `AddCommand(string, ICommandDelegate, bool)` declaration from `ICommandController`

### 3.2.3

- **HandlePipedChunk Signature Change:** `HandlePipedChunk` now accepts `IResult<string>` instead of `string` for full result context access
- **Breaking Change:** Custom commands must update `HandlePipedChunk` signature and use `pipedChunk.Output` to access string value
- **Error Propagation:** Commands can now check `pipedChunk.IsSuccess` and access error details from upstream commands
- **Version bump:** All packages aligned to **3.2.3** (Command, Core, Interface, FileLoader, DependencyInjection, Extensions.Commandline)
- See [CHANGELOG](CHANGELOG.md) and [HandlePipedChunk Migration Guide](docs/changelog-3.2.3-handlePipedChunk-signature-change.md) for full details

### 3.2.2

- **Version bump:** All packages aligned to **3.2.2** (Command, Core, Interface, FileLoader, DependencyInjection, Extensions.Commandline).
- **Defaults:** Builds target .NET 10.0 by default; multi-target `.NET 8.0` via `build.ps1 -UseNet08` (tests auto-skip when multi-targeting).
- **Dependencies:** Xcaciv.Loader 2.1.2, System.IO.Abstractions 22.1.0, Microsoft.Extensions.* 10.0.1, System.CommandLine 2.0.1.
- See [CHANGELOG](CHANGELOG.md) for full details.

### 3.2.1

- **Default TFM:** Builds target .NET 10.0 by default; multi-target `.NET 8.0` via `build.ps1 -UseNet08` (tests auto-skip when multi-targeting).
- **Dependencies:** Xcaciv.Loader 2.1.2, System.IO.Abstractions 22.1.0, Microsoft.Extensions.* 10.0.1, System.CommandLine 2.0.1.
- **Binary compatibility:** `PipelineConfiguration` and `PipelineBackpressureMode` are type-forwarded into `Xcaciv.Command.Interface` to keep existing consumers working.
- See [CHANGELOG](CHANGELOG.md) for full details.

### 3.1.0

- **Type-Safe Parameter System:** Fully generic `IParameterValue<T>` with compile-time type safety
- **Enhanced Security:** Parameters validated before execution, no direct raw string access
- **Breaking:** Commands use `GetValue<T>()` instead of `RawValue`
- **Comprehensive Tests:** 36 new tests for built-in commands (232 total passing)
- **Rich Diagnostics:** Detailed type mismatch and validation error messages
- See [CHANGELOG](CHANGELOG.md) for full details

### 3.0.0

- **BREAKING:** Removed deprecated `EnableDefaultCommands()` - use `RegisterBuiltInCommands()`
- **BREAKING:** Removed deprecated `GetHelp()` - use `await GetHelpAsync()`
- Cleaned up API surface for better maintainability
- See [Migration Guide v3.0](docs/migration_guide_v3.0.md) for upgrade instructions

### 2.1.3

- Bumped all package versions to 2.1.3
- Updated to Xcaciv.Loader 2.1.2
- Documentation improvements
- Maintains .NET 10 targeting

## Documentation

- [CHANGELOG](CHANGELOG.md) - Complete version history and release notes
- [HandlePipedChunk Migration Guide](docs/changelog-3.2.3-handlePipedChunk-signature-change.md) - Version 3.2.3 breaking change guide
- [Command Template](COMMAND_TEMPLATE.md) - Template and guide for implementing new commands
- [Quickstart](docs/learn/quickstart.md) - Five-minute walkthrough
- [Parameter System Implementation](PARAMETER_SYSTEM_IMPLEMENTATION.md) - Type-safe parameter guide
- [Command Test Coverage](COMMAND_TEST_COVERAGE_COMPLETE.md) - Test coverage report
- [RawValue Removal](RAWVALUE_REMOVAL_COMPLETE.md) - Security improvement details
- [Migration Guide v3.0](docs/migration_guide_v3.0.md) - Upgrade instructions
- [Build Guide](BUILD.md) - Build script usage and configuration
- [Security Policy](SECURITY.md) - Security model and vulnerability reporting
- [Architecture Diagram](docs/architecture_diagram.md) - System component overview

## License

AGPL-3.0 License. See LICENSE file for details.
