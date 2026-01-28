using System;
using System.Collections.Concurrent;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Core.Parameters;

/// <summary>
/// Caching wrapper for NamesValidator that provides thread-safe cache for improved performance.
/// Delegates all parsing logic to NamesValidator while adding a caching layer.
/// Use this variant when executing high volumes of commands with repeated patterns.
/// Memory overhead: ~20-50 KB for cache (bounded to 1000 entries).
/// </summary>
public static class NamesValidatorCaching
{
    /// <summary>
    /// Thread-safe cache for validated command names to avoid repeated parsing operations.
    /// Key format: "{commandLine}|{upper}" to handle both case variants.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> ValidatedNameCache = new();

    /// <summary>
    /// Parses and validates a command name from a command line with caching.
    /// Delegates actual parsing to NamesValidator and caches the result.
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

        // Slow path: delegate to NamesValidator for actual parsing
        var result = NamesValidator.GetValidCommandName(commandLine, upper);
        
        // Cache result (bounded: max 1000 entries to prevent unbounded growth)
        if (ValidatedNameCache.Count < 1000)
        {
            ValidatedNameCache.TryAdd(cacheKey, result);
        }
        
        return result;
    }

    /// <summary>
    /// Parses arguments from a command line, excluding the command name itself.
    /// Delegates to NamesValidator for actual parsing - no caching for arguments as they vary widely.
    /// </summary>
    /// <param name="commandLine">Full command line text</param>
    /// <returns>Array of arguments (command name is excluded)</returns>
    public static string[] GetArgumentsFromCommandline(string commandLine)
    {
        // Delegate directly to NamesValidator - argument parsing results are typically unique
        // and don't benefit from caching due to low hit rate
        return NamesValidator.GetArgumentsFromCommandline(commandLine);
    }

    /// <summary>
    /// Clears the validation cache. Useful for testing or memory management.
    /// </summary>
    public static void ClearCache()
    {
        ValidatedNameCache.Clear();
    }
}
