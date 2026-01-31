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

}
