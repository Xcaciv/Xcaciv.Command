using System;
using System.Collections.Generic;

namespace Xcaciv.Command.Interface;

/// <summary>
/// Provides registration and lookup services for command descriptions.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Adds an already constructed command description to the registry.
    /// </summary>
    /// <param name="command">Command metadata to register.</param>
    void AddCommand(ICommandDescription command);

    /// <summary>
    /// Registers a command type by scanning its attributes for metadata.
    /// </summary>
    /// <param name="packageKey">Logical grouping key for the command.</param>
    /// <param name="commandType">The command implementation type.</param>
    /// <param name="modifiesEnvironment">Indicates whether the command mutates the environment.</param>
    void AddCommand(string packageKey, Type commandType, bool modifiesEnvironment = false);

    /// <summary>
    /// Registers a command based on an existing command instance.
    /// </summary>
    /// <param name="packageKey">Logical grouping key for the command.</param>
    /// <param name="command">Command implementation instance.</param>
    /// <param name="modifiesEnvironment">Indicates whether the command mutates the environment.</param>
    void AddCommand(string packageKey, ICommandDelegate command, bool modifiesEnvironment = false);

    /// <summary>
    /// Attempts to locate a command by its base command text.
    /// </summary>
    /// <param name="commandKey">Command name to search for.</param>
    /// <param name="commandDescription">Command metadata if found.</param>
    /// <returns>True when a command is found.</returns>
    bool TryGetCommand(string commandKey, out ICommandDescription? commandDescription);

    /// <summary>
    /// Gets a thread-safe immutable snapshot of all registered commands.
    /// Useful for iteration when concurrent modifications may occur.
    /// </summary>
    /// <returns>Immutable snapshot of command descriptions.</returns>
    IReadOnlyDictionary<string, ICommandDescription> GetCommandSnapshot();

    /// <summary>
    /// Returns all registered commands (root commands only).
    /// </summary>
    IEnumerable<ICommandDescription> GetAllCommands();

    /// <summary>
    /// Builds a composite environment context by crawling all registered commands
    /// and collecting their default environment values via <see cref="ICommandDelegate.GetDefaultEnvironment"/>.
    /// </summary>
    /// <param name="commandFactory">Factory used to instantiate commands for retrieving their default environments.</param>
    /// <returns>An <see cref="IControllerEnvironmentContext"/> populated with command-scoped defaults.</returns>
    IControllerEnvironmentContext GetEnvironment(ICommandFactory commandFactory);
}
