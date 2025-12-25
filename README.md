# Xcaciv.Command: Excessive Command Framework

Excessively modular, async pipeable, command framework.

```csharp
    var controller = new Xc.Command.CommandController();
    controller.EnableDefaultCommands();

    _ = controller.Run("Say Hello to my little friend");

    // outputs: Hello to my little friend
```

Commands are .NET class libraries that contain implementations of the `Xc.Command.ICommand` interface and are decorated with attributes that describe the command and its parameters. These attributes are used to filter and validate input as well as output auto-generated help for the user.

## Features

- **Async Pipeline Support**: Commands can be chained with `|` for threaded pipeline execution
- **Plugin System**: Dynamic command loading from external assemblies
- **Secure Plugin Loading**: Built on Xcaciv.Loader 2.1.0 with instance-based security policies
- **Auto-Generated Help**: Comprehensive help generation from command attributes
- **Sub-Commands**: Hierarchical command structure support
- **Built-in Commands**: `SAY`, `SET`, `ENV`, and `REGIF` included

## Dependencies

- **Xcaciv.Loader 2.1.0**: Assembly loading with instance-based security configuration
- **System.IO.Abstractions 21.0.2**: File system abstraction for testability

## Security

This framework uses Xcaciv.Loader 2.1.0's instance-based security policies:
- **Default Security Policy**: Each plugin is restricted to its own directory
- **No Wildcard Access**: Replaces v1.x wildcard (`*`) restrictions with proper path-based security
- **Security Exception Handling**: Graceful handling of security violations with detailed logging
- **Per-Instance Configuration**: Each AssemblyContext has independent security settings

For detailed security model, plugin development guidelines, and vulnerability reporting procedures, see `SECURITY.md`.

### Audit Logging

Command execution and environment changes can be logged via `IAuditLogger`:

```csharp
// Example: Using audit logging
var controller = new CommandController();
controller.EnableDefaultCommands();
var env = new EnvironmentContext();

// Provide an audit logger implementation
var auditLogger = new YourAuditLoggerImplementation();
controller.AuditLogger = auditLogger;

// Command execution and environment changes are now logged
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
- [X] Migrate to Xcaciv.Loader 2.1.0 with instance-based security

## Version History

### 2.1.0 (Current)
- Bumped all package versions to 2.1.0
- Documentation aligned to Xcaciv.Loader 2.1.0
- Maintains .NET 8 targeting and security policies

## License

BSD-3-Clause
