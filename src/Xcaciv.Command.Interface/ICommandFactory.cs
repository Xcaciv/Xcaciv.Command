using System;

namespace Xcaciv.Command.Interface;

/// <summary>
/// Creates command instances while supporting dependency injection scenarios.
/// </summary>
public interface ICommandFactory
{
    /// <summary>
    /// Constructs a command instance from a command description, handling sub-command routing as needed.
    /// </summary>
    /// <param name="commandDescription">Command metadata.</param>
    /// <param name="ioContext">IO context containing the current parameter set.</param>
    /// <returns>Instantiated command implementation.</returns>
    ICommandDelegate CreateCommand(ICommandDescription commandDescription, IIoContext ioContext);

    /// <summary>
    /// Constructs a command instance directly from type metadata.
    /// </summary>
    /// <param name="fullTypeName">Fully-qualified type name.</param>
    /// <param name="packagePath">Path to the assembly that contains the type.</param>
    /// <returns>Instantiated command implementation.</returns>
    ICommandDelegate CreateCommand(string fullTypeName, string packagePath);

    /// <summary>
    /// Asynchronously constructs a command instance from a command description, handling sub-command routing.
    /// This method avoids blocking on SetParameters and is preferred for pipeline/executor contexts.
    /// </summary>
    /// <param name="commandDescription">Command metadata.</param>
    /// <param name="ioContext">IO context containing the current parameter set.</param>
    /// <returns>Instantiated command implementation.</returns>
    Task<ICommandDelegate> CreateCommandAsync(ICommandDescription commandDescription, IIoContext ioContext);
}
