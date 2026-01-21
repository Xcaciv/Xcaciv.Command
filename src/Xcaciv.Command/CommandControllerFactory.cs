using System;
using System.Linq;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command;

/// <summary>
/// Factory for creating CommandController instances without requiring a DI framework.
/// Provides convenient methods to configure and initialize the controller with options.
/// </summary>
public static class CommandControllerFactory
{
    /// <summary>
    /// Create a CommandController using provided options.
    /// </summary>
    /// <param name="options">Optional controller options. If null, defaults are used.</param>
    /// <returns>A configured CommandController instance.</returns>
    public static CommandController Create(CommandControllerOptions? options = null)
    {
        options ??= new CommandControllerOptions();

        CommandController controller;

        if (!string.IsNullOrWhiteSpace(options.RestrictedDirectory))
        {
            // Use restricted directory constructor to enforce base path restriction
            controller = new CommandController(new Crawler(), options.RestrictedDirectory!);
        }
        else
        {
            // Use default constructor which wires core components
            controller = new CommandController();
        }

        // Enable built-in commands
        if (options.EnableDefaultCommands)
        {
            controller.RegisterBuiltInCommands();
        }

        // Add package directories and load commands
        if (options.PackageDirectories != null)
        {
            foreach (var dir in options.PackageDirectories.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                controller.AddPackageDirectory(dir);
            }
            controller.LoadCommands();
        }

        return controller;
    }

    /// <summary>
    /// Create a CommandController with explicit constructor-injected components (no DI framework required).
    /// </summary>
    /// <param name="commandRegistry">Registry instance.</param>
    /// <param name="commandLoader">Loader instance.</param>
    /// <param name="pipelineExecutor">Pipeline executor.</param>
    /// <param name="commandExecutor">Command executor.</param>
    /// <param name="commandFactory">Command factory.</param>
    /// <param name="auditLogger">Optional audit logger to set on the controller.</param>
    /// <param name="outputEncoder">Optional output encoder to set on the controller.</param>
    /// <param name="serviceProvider">Optional service provider for factory-based activations.</param>
    /// <returns>A fully-wired CommandController.</returns>
    public static CommandController Create(
        ICommandRegistry commandRegistry,
        ICommandLoader commandLoader,
        IPipelineExecutor pipelineExecutor,
        ICommandExecutor commandExecutor,
        ICommandFactory commandFactory,
        IAuditLogger? auditLogger = null,
        IOutputEncoder? outputEncoder = null,
        IServiceProvider? serviceProvider = null)
    {
        if (commandRegistry == null) throw new ArgumentNullException(nameof(commandRegistry));
        if (commandLoader == null) throw new ArgumentNullException(nameof(commandLoader));
        if (pipelineExecutor == null) throw new ArgumentNullException(nameof(pipelineExecutor));
        if (commandExecutor == null) throw new ArgumentNullException(nameof(commandExecutor));
        if (commandFactory == null) throw new ArgumentNullException(nameof(commandFactory));

        var controller = new CommandController(
            commandRegistry,
            commandLoader,
            pipelineExecutor,
            commandExecutor,
            commandFactory,
            serviceProvider);

        if (auditLogger != null)
        {
            controller.AuditLogger = auditLogger;
        }

        if (outputEncoder != null)
        {
            controller.OutputEncoder = outputEncoder;
        }

        return controller;
    }
}
