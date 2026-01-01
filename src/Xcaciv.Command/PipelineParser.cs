using System;
using System.Collections.Generic;
using System.Text;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command;

/// <summary>
/// Formal parser for pipeline syntax that handles quoted arguments and escape sequences.
/// Supports:
/// - Double quotes for grouping arguments with spaces: "arg with spaces"
/// - Single quotes for literal strings: 'literal "quoted"'
/// - Backslash escape sequences: \| for literal pipe, \" for quote in string
/// - Pipeline delimiter: | (unquoted)
/// </summary>
public class PipelineParser
{
    /// <summary>
    /// Parse pipeline command line into individual command segments.
    /// </summary>
    /// <param name="commandLine">Full pipeline command line.</param>
    /// <returns>Array of command segments (unquoted, escape sequences resolved).</returns>
    /// <exception cref="InvalidOperationException">When quotes or escapes are unbalanced.</exception>
    public static string[] ParsePipeline(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return Array.Empty<string>();
        }

        // divide string on pipeline separator
        // validate quoted strings

        var segments = new List<string>();
        var currentSegment = new StringBuilder();
        var i = 0;

        while (i < commandLine.Length)
        {
            var ch = commandLine[i];

            // Handle escape sequences
            if (ch == CommandSyntax.EscapeChar && i + 1 < commandLine.Length)
            {
                var nextChar = commandLine[i + 1];
                // Unescape special characters
                if (nextChar == CommandSyntax.PipelineDelimiter || nextChar == CommandSyntax.DoubleQuote || nextChar == CommandSyntax.SingleQuote || nextChar == CommandSyntax.EscapeChar)
                {
                    currentSegment.Append(nextChar);
                    i += 2;
                    continue;
                }
            }

            // Handle unquoted pipe delimiter (segment boundary)
            if (ch == CommandSyntax.PipelineDelimiter)
            {
                var segment = currentSegment.ToString().Trim();
                if (!string.IsNullOrEmpty(segment))
                {
                    segments.Add(segment);
                }
                currentSegment.Clear();
                i++;
                continue;
            }

            // Handle double-quoted strings (allow whitespace and special chars)
            if (ch == CommandSyntax.DoubleQuote)
            {
                i = ParseQuotedString(commandLine, i, CommandSyntax.DoubleQuote, currentSegment);
                continue;
            }

            // Handle single-quoted strings (literal, no escape processing)
            if (ch == CommandSyntax.SingleQuote)
            {
                i = ParseQuotedString(commandLine, i, CommandSyntax.SingleQuote, currentSegment);
                continue;
            }

            // Regular character
            currentSegment.Append(ch);
            i++;
        }

        var finalSegment = currentSegment.ToString().Trim();
        if (!string.IsNullOrEmpty(finalSegment))
        {
            segments.Add(finalSegment);
        }

        return segments.ToArray();
    }

    /// <summary>
    /// Parse a quoted string, handling escape sequences within double quotes.
    /// Single-quoted strings are treated as literals (no escape processing).
    /// </summary>
    private static int ParseQuotedString(string input, int startIndex, char quoteChar, StringBuilder output)
    {
        var i = startIndex + 1; // Skip opening quote
        var isDoubleQuoted = quoteChar == CommandSyntax.DoubleQuote;

        while (i < input.Length)
        {
            var ch = input[i];

            // Handle escape sequences in double-quoted strings
            if (isDoubleQuoted && ch == CommandSyntax.EscapeChar && i + 1 < input.Length)
            {
                var nextChar = input[i + 1];
                if (nextChar == CommandSyntax.DoubleQuote || nextChar == CommandSyntax.EscapeChar || nextChar == CommandSyntax.PipelineDelimiter)
                {
                    output.Append(nextChar);
                    i += 2;
                    continue;
                }
            }

            // End quote reached
            if (ch == quoteChar)
            {
                return i + 1; // Skip closing quote
            }

            // Regular character in quoted string
            output.Append(ch);
            i++;
        }

        throw new InvalidOperationException($"Unbalanced {quoteChar} quote in pipeline.");
    }
}
