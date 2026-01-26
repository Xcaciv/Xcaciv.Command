using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Loader;

namespace Xcaciv.Command;

/// <summary>
/// Default implementation of IHelpService that formats help output from command metadata via reflection.
/// Extracts metadata from command attributes and produces standardized help output.
/// </summary>
public class HelpService : IHelpService
{
    // Cache for loaded types to avoid repeated assembly loads during help generation
    private readonly ConcurrentDictionary<string, Type?> _typeCache = new();
    public string BuildHelp(ICommandDelegate command, string[] parameters, IEnvironmentContext environment)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if the command has a custom Help() implementation (not from AbstractCommand)
        // by checking if the declaring type is not AbstractCommand
        var commandType = command.GetType();

        // Otherwise, build help from attributes

        if (Attribute.GetCustomAttribute(commandType, typeof(CommandRegisterAttribute)) is not CommandRegisterAttribute baseCommand)
        {
            throw new InvalidOperationException("CommandRegisterAttribute is required for all commands");
        }

        var commandParametersOrdered = GetOrderedParameters(commandType, false);
        var commandParametersFlag = GetFlagParameters(commandType);
        var commandParametersNamed = GetNamedParameters(commandType, false);
        var commandParametersSuffix = GetSuffixParameters(commandType, false);

        var builder = new StringBuilder();

        // Command name with parent if applicable
        if (Attribute.GetCustomAttribute(commandType, typeof(CommandRootAttribute)) is CommandRootAttribute rootCommand)
        {
            builder.Append($"{rootCommand.Command} ");
        }
        
        builder.AppendLine($"{baseCommand.Command}:");
        builder.AppendLine($"  {baseCommand.Description}");
        builder.AppendLine();
        builder.AppendLine("Usage:");
        
        if (commandParametersOrdered.Length + commandParametersNamed.Length + commandParametersSuffix.Length + commandParametersFlag.Length > 0)
        {
            var parameterBuilder = new StringBuilder();
            var prototypeBuilder = new StringBuilder();

            // Ordered parameters
            foreach (var parameter in commandParametersOrdered)
            {
                parameterBuilder.AppendLine($"  {parameter}");
                prototypeBuilder.Append($"{parameter.GetIndicator()} ");
            }

            // Flag parameters
            foreach (var parameter in commandParametersFlag)
            {
                parameterBuilder.AppendLine($"  {parameter}");
                prototypeBuilder.Append($"{parameter.GetIndicator()} ");
            }

            // Named parameters
            foreach (var parameter in commandParametersNamed)
            {
                parameterBuilder.AppendLine($"  {parameter}");
                string valueIndicator = (parameter.AllowedValues.Length > 0) ?
                    $"[{string.Join("|", parameter.AllowedValues)}]" :
                    parameter.Name;

                prototypeBuilder.Append($"{parameter.GetIndicator()} <{valueIndicator}> ");
            }

            // Suffix parameters
            foreach (var parameter in commandParametersSuffix)
            {
                parameterBuilder.AppendLine($"  {parameter}");
                prototypeBuilder.Append($"{parameter.GetIndicator()} ");
            }

            // Use prototype from attribute or generated one
            if (baseCommand.Prototype.Equals("todo", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append(prototypeBuilder).AppendLine();
            }
            else
            {
                builder.AppendLine($"  {baseCommand.Prototype}");
            }

            builder.AppendLine();
            builder.AppendLine("Options:");
            builder.Append(parameterBuilder).AppendLine();
        }

        // Remarks section
        if (Attribute.GetCustomAttributes(commandType, typeof(CommandHelpRemarksAttribute)) is CommandHelpRemarksAttribute[] helpRemarks && helpRemarks.Length > 0)
        {
            builder.AppendLine("Remarks:");
            foreach (var remark in helpRemarks)
            {
                builder.AppendLine();
                builder.AppendLine(remark.Remarks);
            }
        }

        builder.AppendLine();
        return builder.ToString();
    }

    public string BuildOneLineHelp(ICommandDescription commandDescription)
    {
        if (commandDescription == null) throw new ArgumentNullException(nameof(commandDescription));

        if (commandDescription.SubCommands.Count > 0)
        {
            return $"-\t{commandDescription.BaseCommand,-12} [Has sub-commands]";
        }

        // Get the command type using the cache to avoid repeated assembly loads
        var commandType = GetCommandType(commandDescription.FullTypeName, commandDescription.PackageDescription?.FullPath);

        if (commandType != null)
        {
            // Check if command has CommandRootAttribute (indicates it's a sub-command)
            if (Attribute.GetCustomAttribute(commandType, typeof(CommandRootAttribute)) is CommandRootAttribute)
            {
                var baseCommand = Attribute.GetCustomAttribute(commandType, typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;
                return $"-\t{commandDescription.BaseCommand,-12} {baseCommand?.Description ?? ""}";
            }

            // For regular commands, use CommandRegisterAttribute
            var registerAttr = Attribute.GetCustomAttribute(commandType, typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;
            if (registerAttr != null)
            {
                return $"{registerAttr.Command,-12} {registerAttr.Description}";
            }
        }

        return $"{commandDescription.BaseCommand,-12} [No description available]";
    }

    public bool IsHelpRequest(string[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
        {
            return false;
        }

        return parameters.Any(p => p.Equals("--HELP", StringComparison.OrdinalIgnoreCase) ||
                                   p.Equals("-?", StringComparison.OrdinalIgnoreCase) ||
                                   p.Equals("/?", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets or loads the Type for a command, using a cache to avoid repeated assembly loads.
    /// </summary>
    /// <param name="fullTypeName">The full type name of the command.</param>
    /// <param name="assemblyPath">Optional assembly path for plugin types.</param>
    /// <returns>The Type if found, otherwise null.</returns>
    private Type? GetCommandType(string fullTypeName, string? assemblyPath)
    {
        // Create composite key that includes assembly path to handle same type names in different assemblies
        var cacheKey = string.IsNullOrEmpty(assemblyPath) 
            ? fullTypeName 
            : $"{fullTypeName}|{assemblyPath}";
        
        // Use GetOrAdd to atomically check cache and load if needed
        return _typeCache.GetOrAdd(cacheKey, _ =>
        {
            // First try Type.GetType (works for built-in types)
            var type = Type.GetType(fullTypeName);
            
            // If Type.GetType fails (common for plugin types), try loading from assembly using AssemblyContext
            if (type == null && !string.IsNullOrEmpty(assemblyPath))
            {
                try
                {
                    // Use AssemblyContext to enforce security restrictions consistent with CommandFactory
                    var basePathRestriction = Path.GetDirectoryName(assemblyPath);
                    
                    // Only proceed if we have a valid directory path for security enforcement
                    if (string.IsNullOrEmpty(basePathRestriction))
                    {
                        Trace.WriteLine(
                            $"Cannot load type [{fullTypeName}] from [{assemblyPath}]: Invalid path for security enforcement");
                        return type;
                    }
                    
                    using var context = new AssemblyContext(
                        assemblyPath,
                        basePathRestriction: basePathRestriction,
                        securityPolicy: AssemblySecurityPolicy.Strict);

                    var loadedTypes = context.GetTypes() ?? Enumerable.Empty<Type>();
                    type = loadedTypes.FirstOrDefault(t => t.FullName == fullTypeName);
                }
                catch (SecurityException ex)
                {
                    // Log security violations for diagnostics
                    Trace.WriteLine(
                        $"Security violation loading type [{fullTypeName}] from [{assemblyPath}]: {ex.Message}");
                }
                catch (FileNotFoundException ex)
                {
                    // Log file not found for diagnostics
                    Trace.WriteLine(
                        $"Assembly not found for type [{fullTypeName}] at [{assemblyPath}]: {ex.Message}");
                }
                catch (FileLoadException ex)
                {
                    // Log file load issues for diagnostics
                    Trace.WriteLine(
                        $"Failed to load assembly for type [{fullTypeName}] from [{assemblyPath}]: {ex.Message}");
                }
                catch (BadImageFormatException ex)
                {
                    // Log bad image format for diagnostics
                    Trace.WriteLine(
                        $"Invalid assembly format for type [{fullTypeName}] at [{assemblyPath}]: {ex.Message}");
                }
            }
            
            return type;
        });
    }

    private static CommandParameterOrderedAttribute[] GetOrderedParameters(Type commandType, bool hasPipedInput)
    {
        var ordered = Attribute.GetCustomAttributes(commandType, typeof(CommandParameterOrderedAttribute)) as CommandParameterOrderedAttribute[] ?? Array.Empty<CommandParameterOrderedAttribute>();
        if (hasPipedInput)
        {
            ordered = ordered.Where(x => !x.UsePipe).ToArray();
        }
        return ordered;
    }

    private static CommandParameterNamedAttribute[] GetNamedParameters(Type commandType, bool hasPipedInput)
    {
        var named = Attribute.GetCustomAttributes(commandType, typeof(CommandParameterNamedAttribute)) as CommandParameterNamedAttribute[] ?? Array.Empty<CommandParameterNamedAttribute>();
        if (hasPipedInput)
        {
            named = named.Where(x => !x.UsePipe).ToArray();
        }
        return named;
    }

    private static CommandFlagAttribute[] GetFlagParameters(Type commandType)
    {
        return Attribute.GetCustomAttributes(commandType, typeof(CommandFlagAttribute)) as CommandFlagAttribute[] ?? Array.Empty<CommandFlagAttribute>();
    }

    private static CommandParameterSuffixAttribute[] GetSuffixParameters(Type commandType, bool hasPipedInput)
    {
        var suffix = Attribute.GetCustomAttributes(commandType, typeof(CommandParameterSuffixAttribute)) as CommandParameterSuffixAttribute[] ?? Array.Empty<CommandParameterSuffixAttribute>();
        if (hasPipedInput)
        {
            suffix = suffix.Where(x => !x.UsePipe).ToArray();
        }
        return suffix;
    }
}
