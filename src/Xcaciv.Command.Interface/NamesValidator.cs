using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xcaciv.Command.Interface;

/// <summary>
/// Utility class for validating and parsing command names and arguments.
/// Provides centralized command name normalization logic.
/// </summary>
public static class NamesValidator
{
    /// <summary>
    /// Regex for cleansing command names - only allows alphanumeric, dashes, underscores, and spaces
    /// </summary>
    private static readonly Regex InvalidCommandChars = new Regex(@"[^-_\da-zA-Z ]+", RegexOptions.Compiled);
    
    /// <summary>
    /// Regex for cleansing parameters
    /// </summary>
    private static readonly Regex InvalidParameterChars = new Regex(@"[^-_\da-zA-Z .*?\[\]|""~!@#$%^&*\(\)]+", RegexOptions.Compiled);

    /// <summary>
    /// Thread-safe cache for validated command names to avoid repeated regex operations.
    /// Key format: "{commandLine}|{upper}" to handle both case variants.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> ValidatedNameCache = new();

    /// <summary>
    /// Parses and validates a command name from a command line.
    /// Extracts the first word, removes invalid characters, and normalizes case.
    /// Uses caching to avoid repeated regex operations for common command names.
    /// </summary>
    /// <param name="commandLine">Full command line text</param>
    /// <param name="upper">If true, converts to uppercase; if false, converts to lowercase</param>
    /// <returns>Validated and normalized command name</returns>
    public static string GetValidCommandName(string commandLine, bool upper = true)
    {
        // Fast path: check cache first
        var cacheKey = $"{commandLine}|{upper}";
        if (ValidatedNameCache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        // Slow path: perform validation and cache result
        commandLine = commandLine.Trim();
        var commandText = (commandLine.Contains(' ') ?
                commandLine.Substring(0, commandLine.Trim().IndexOf(' '))
                 : commandLine).Trim('-');
        
        // remove invalid characters
        commandText = InvalidCommandChars.Replace(commandText.Trim(), "");
        
        // set proper case
        var result = upper ? commandText.ToUpper() : commandText.ToLower();
        
        // Cache result (bounded: max 1000 entries to prevent unbounded growth)
        if (ValidatedNameCache.Count < 1000)
        {
            ValidatedNameCache.TryAdd(cacheKey, result);
        }
        
        return result;
    }

    /// <summary>
    /// Parses arguments from a command line, excluding the command name itself.
    /// Handles quoted strings and special characters.
    /// </summary>
    /// <param name="commandLine">Full command line text</param>
    /// <returns>Array of arguments (command name is excluded)</returns>
    public static string[] GetArgumentsFromCommandline(string commandLine)
    {
        var args = Regex.Matches(commandLine, @"[\""].*?[\""]|[\w-]+")
            .Cast<Match>()
            .Select(o => InvalidParameterChars.Replace(o.Value, "").Trim('"'))
            .ToArray();

        // the first item in the array is the command
        return args.Skip(1).ToArray();
    }
}
