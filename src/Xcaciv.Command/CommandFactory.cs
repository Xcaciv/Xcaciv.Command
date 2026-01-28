using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Loader;

namespace Xcaciv.Command;

/// <summary>
/// Default command factory that supports optional dependency injection resolution.
/// Provides both sync and async methods for command instantiation.
/// Leverages Xcaciv.Loader 2.1.1 instance-based security.
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider? _serviceProvider;
    private AssemblySecurityConfiguration _securityConfiguration;

    public CommandFactory(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
        _securityConfiguration = new AssemblySecurityConfiguration();
    }

    /// <summary>
    /// Configure assembly loading security policies.
    /// Leverages Xcaciv.Loader 2.1.1 instance-based security with per-assembly context isolation.
    /// </summary>
    /// <param name="configuration">Security configuration with policy, path restrictions, and allowlists</param>
    public void SetSecurityConfiguration(AssemblySecurityConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        configuration.Validate();
        _securityConfiguration = configuration;
    }

    public ICommandDelegate CreateCommand(ICommandDescription commandDescription, IIoContext ioContext)
    {
        if (commandDescription == null) throw new ArgumentNullException(nameof(commandDescription));
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));

        if (commandDescription.SubCommands.Count > 0 &&
            ioContext.Parameters != null &&
            ioContext.Parameters.Length > 0)
        {
            // Normalize sub-command key to uppercase for consistent lookup
            var subCommandKey = NamesValidator.GetValidCommandName(ioContext.Parameters[0], upper: true);
            
            if (commandDescription.SubCommands.TryGetValue(subCommandKey, out var subCommandDescription) &&
                subCommandDescription != null)
            {
                // SetParameters is synchronous (returns Task.CompletedTask) per AbstractTextIo implementation.
                // Use GetAwaiter().GetResult() safely since task is already completed.
                var setParamsTask = ioContext.SetParameters(ioContext.Parameters[1..]);
                if (!setParamsTask.IsCompleted)
                {
                    // Fallback: if implementation is truly async, wait synchronously
                    // This maintains backward compatibility but logs the anomaly
                    setParamsTask.ConfigureAwait(false).GetAwaiter().GetResult();
                }
                return CreateCommand(subCommandDescription.FullTypeName, commandDescription.PackageDescription.FullPath);
            }
            else
            {
                // Sub-command not found - provide detailed error message
                var availableSubCommands = string.Join(", ", commandDescription.SubCommands.Keys);
                throw new InvalidOperationException(
                    $"Sub-command '{subCommandKey}' not found under root command '{commandDescription.BaseCommand}'. " +
                    $"Available sub-commands: {availableSubCommands}. " +
                    $"Note: Sub-command names are case-insensitive.");
            }
        }

        // No sub-commands or no parameters provided - try to execute root command directly
        if (string.IsNullOrWhiteSpace(commandDescription.FullTypeName))
        {
            if (commandDescription.SubCommands.Count > 0)
            {
                // This is a root command with sub-commands but no direct implementation
                var availableSubCommands = string.Join(", ", commandDescription.SubCommands.Keys);
                throw new InvalidOperationException(
                    $"Command '{commandDescription.BaseCommand}' requires a sub-command. " +
                    $"Available sub-commands: {availableSubCommands}. " +
                    $"Usage: {commandDescription.BaseCommand} <sub-command> [options]");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Command '{commandDescription.BaseCommand}' has no implementation type defined.");
            }
        }
        
        return CreateCommand(commandDescription.FullTypeName, commandDescription.PackageDescription.FullPath);
    }

    public ICommandDelegate CreateCommand(string fullTypeName, string packagePath)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            throw new InvalidOperationException("Command type name is empty.");
        }

        var commandType = Type.GetType(fullTypeName);
        if (commandType != null)
        {
            if (_serviceProvider?.GetService(commandType) is ICommandDelegate resolvedDelegate)
            {
                return resolvedDelegate;
            }

            return AssemblyContext.ActivateInstance<ICommandDelegate>(commandType);
        }

        if (string.IsNullOrWhiteSpace(packagePath))
        {
            throw new InvalidOperationException($"Command [{fullTypeName}] is not loaded and no assembly was defined.");
        }

        try
        {
            // Xcaciv.Loader 2.1.1: Instance-based security with strict path restrictions
            // Each plugin command is loaded in an isolated context sandboxed to its own directory
            var basePathRestriction = _securityConfiguration.EnforceBasePathRestriction
                ? Path.GetDirectoryName(packagePath) ?? Directory.GetCurrentDirectory()
                : ".";

            // Xcaciv.Loader 2.1.1: Determine effective security policy
            // If AllowReflectionEmit is disabled, force Strict policy regardless of configuration
            var effectivePolicy = _securityConfiguration.AllowReflectionEmit
                ? _securityConfiguration.SecurityPolicy
                : AssemblySecurityPolicy.Strict;

            // Xcaciv.Loader 2.1.1: Create isolated assembly loading context with security policy
            using var context = new AssemblyContext(
                packagePath,
                basePathRestriction: basePathRestriction,
                securityPolicy: effectivePolicy);
                
            return context.CreateInstance<ICommandDelegate>(fullTypeName);
        }
        catch (SecurityException ex)
        {
            throw new InvalidOperationException(
                $"[Xcaciv.Loader 2.1.1] Security violation loading command [{fullTypeName}] from [{packagePath}]: " +
                $"Policy={(_securityConfiguration.AllowReflectionEmit ? _securityConfiguration.SecurityPolicy : AssemblySecurityPolicy.Strict)}, " +
                $"EnforceBasePathRestriction={_securityConfiguration.EnforceBasePathRestriction}, " +
                $"BasePathRestriction={Path.GetDirectoryName(packagePath)}. " +
                $"Details: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            throw new InvalidOperationException(
                $"[Xcaciv.Loader 2.1.1] Failed to load command [{fullTypeName}] from [{packagePath}]: {ex.GetType().Name}. " +
                $"Verify plugin assembly exists and is valid. {ex.Message}", ex);
        }
    }

    public async Task<ICommandDelegate> CreateCommandAsync(ICommandDescription commandDescription, IIoContext ioContext)
    {
        if (commandDescription == null) throw new ArgumentNullException(nameof(commandDescription));
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));

        if (commandDescription.SubCommands.Count > 0 &&
            ioContext.Parameters != null &&
            ioContext.Parameters.Length > 0 &&
            commandDescription.SubCommands.TryGetValue(ioContext.Parameters[0].ToUpper(), out var subCommandDescription) &&
            subCommandDescription != null)
        {
            // Use async SetParameters to avoid blocking
            await ioContext.SetParameters(ioContext.Parameters[1..]).ConfigureAwait(false);
            return CreateCommand(subCommandDescription.FullTypeName, commandDescription.PackageDescription.FullPath);
        }

        return CreateCommand(commandDescription.FullTypeName, commandDescription.PackageDescription.FullPath);
    }
}
