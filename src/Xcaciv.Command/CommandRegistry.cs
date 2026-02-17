using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;

namespace Xcaciv.Command;

/// <summary>
/// In-memory registry that tracks available commands and their metadata.
/// Thread-safe for concurrent reads and writes via ConcurrentDictionary.
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, ICommandDescription> _commands = new();

    public void AddCommand(ICommandDescription command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        if (command.SubCommands.Count > 0 &&
            _commands.TryGetValue(command.BaseCommand, out var existingDescription))
        {
            foreach (var subCommand in command.SubCommands)
            {
                existingDescription.SubCommands[subCommand.Key] = subCommand.Value;
            }
        }
        else
        {
            _commands[command.BaseCommand] = command;
        }
    }

    public void AddCommand(string packageKey, Type commandType, bool modifiesEnvironment = false)
    {
        if (commandType == null) throw new ArgumentNullException(nameof(commandType));

        if (Attribute.GetCustomAttribute(commandType, typeof(CommandRegisterAttribute)) is not CommandRegisterAttribute)
        {
            Trace.WriteLine(
                $"{commandType.FullName} implements ICommandDelegate but does not have CommandRegisterAttribute. Unable to automatically register.");
            return;
        }

        var packageDesc = new PackageDescription
        {
            Name = packageKey,
            FullPath = commandType.Assembly.Location
        };

        var commandParameters = new CommandParameters();
        var commandDesc = commandParameters.CreatePackageDescription(commandType, packageDesc);
        if (commandDesc is CommandDescription mutableDescription)
        {
            mutableDescription.ModifiesEnvironment = modifiesEnvironment;
        }
        AddCommand(commandDesc);
    }

    public void AddCommand(string packageKey, ICommandDelegate command, bool modifiesEnvironment = false)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        AddCommand(packageKey, command.GetType(), modifiesEnvironment);
    }

    public bool TryGetCommand(string commandKey, out ICommandDescription? commandDescription)
    {
        if (commandKey == null) throw new ArgumentNullException(nameof(commandKey));
        return _commands.TryGetValue(commandKey, out commandDescription);
    }

    public IReadOnlyDictionary<string, ICommandDescription> GetCommandSnapshot()
    {
        return new ReadOnlyDictionary<string, ICommandDescription>(
            new Dictionary<string, ICommandDescription>(_commands));
    }

    public IEnumerable<ICommandDescription> GetAllCommands()
    {
        return new List<ICommandDescription>(_commands.Values);
    }

    /// <inheritdoc />
    public IControllerEnvironmentContext GetEnvironment(ICommandFactory commandFactory)
    {
        ArgumentNullException.ThrowIfNull(commandFactory);

        var controllerEnvironment = new ControllerEnvironmentContext();
        foreach (var commandDescription in GetAllCommands())
        {
            ApplyDefaultEnvironment(controllerEnvironment, commandDescription, commandFactory);
        }

        return controllerEnvironment;
    }

    private static void ApplyDefaultEnvironment(IControllerEnvironmentContext controllerEnvironment, ICommandDescription commandDescription, ICommandFactory commandFactory, int depth = 0)
    {
        if (!string.IsNullOrWhiteSpace(commandDescription.FullTypeName))
        {
            AddCommandDefaults(controllerEnvironment, commandDescription, commandFactory);
        }

        if (commandDescription.SubCommands.Count == 0)
        {
            return;
        }

        foreach (var subCommand in commandDescription.SubCommands.Values)
        {
            AddCommandDefaults(controllerEnvironment, subCommand, commandFactory);
        }
    }

    private static void AddCommandDefaults(IControllerEnvironmentContext controllerEnvironment, ICommandDescription commandDescription, ICommandFactory commandFactory)
    {
        var commandInstance = commandFactory.CreateCommand(commandDescription.FullTypeName, commandDescription.PackageDescription.FullPath);
        try
        {
            var defaultEnvironment = commandInstance.GetDefaultEnvironment();
            if (defaultEnvironment.Count == 0)
            {
                return;
            }

            var commandName = NamesValidator.GetValidCommandName(commandDescription.BaseCommand);
            controllerEnvironment.UpdateEnvironment(defaultEnvironment, commandName);
        }
        finally
        {
            commandInstance
                .DisposeAsync()
                .AsTask()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
