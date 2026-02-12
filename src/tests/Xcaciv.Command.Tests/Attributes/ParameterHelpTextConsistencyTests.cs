using System;
using Xunit;
using Xcaciv.Command.Interface.Attributes;

namespace Xcaciv.Command.Tests.Attributes
{
    /// <summary>
    /// Tests to ensure consistent help text behavior between ordered and named parameters,
    /// particularly for displaying allowed values.
    /// </summary>
    public class ParameterHelpTextConsistencyTests
    {
        [Fact]
        public void OrderedParameter_WithAllowedValues_ShowsInHelpText()
        {
            // Arrange
            var attribute = new CommandParameterOrderedAttribute("mode", "Operation mode")
            {
                AllowedValues = new[] { "read", "write", "append" }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Contains("Operation mode", helpText);
            Assert.Contains("Allowed values:", helpText);
            Assert.Contains("read", helpText);
            Assert.Contains("write", helpText);
            Assert.Contains("append", helpText);
        }

        [Fact]
        public void OrderedParameter_WithoutAllowedValues_ShowsOnlyDescription()
        {
            // Arrange
            var attribute = new CommandParameterOrderedAttribute("filename", "The file to process");

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Equal("The file to process", helpText);
            Assert.DoesNotContain("Allowed values:", helpText);
        }

        [Fact]
        public void NamedParameter_WithAllowedValues_ShowsInHelpText()
        {
            // Arrange
            var attribute = new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                AllowedValues = new[] { "quiet", "normal", "detailed" }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Contains("Log verbosity level", helpText);
            Assert.Contains("Allowed values:", helpText);
            Assert.Contains("quiet", helpText);
            Assert.Contains("normal", helpText);
            Assert.Contains("detailed", helpText);
        }

        [Fact]
        public void OrderedAndNamedParameters_HaveConsistentHelpFormat()
        {
            // Arrange
            var orderedAttr = new CommandParameterOrderedAttribute("mode", "Mode selection")
            {
                AllowedValues = new[] { "a", "b", "c" }
            };

            var namedAttr = new CommandParameterNamedAttribute("format", "Format selection")
            {
                AllowedValues = new[] { "x", "y", "z" }
            };

            // Act
            var orderedHelp = orderedAttr.GetValueDescription();
            var namedHelp = namedAttr.GetValueDescription();

            // Assert - Both should include "Allowed values:" prefix
            Assert.Contains("(Allowed values:", orderedHelp);
            Assert.Contains("(Allowed values:", namedHelp);
            
            // Both should end with closing parenthesis
            Assert.EndsWith(")", orderedHelp);
            Assert.EndsWith(")", namedHelp);
        }

        [Fact]
        public void OrderedParameter_ToString_IncludesAllowedValues()
        {
            // Arrange
            var attribute = new CommandParameterOrderedAttribute("config", "Configuration type")
            {
                AllowedValues = new[] { "dev", "staging", "prod" }
            };

            // Act
            var fullHelpText = attribute.ToString();

            // Assert - ToString uses GetValueDescription, so should include allowed values
            Assert.Contains("Allowed values:", fullHelpText);
            Assert.Contains("dev, staging, prod", fullHelpText);
        }

        [Fact]
        public void OrderedParameter_WithEmptyAllowedValues_ShowsOnlyDescription()
        {
            // Arrange
            var attribute = new CommandParameterOrderedAttribute("param", "Some parameter")
            {
                AllowedValues = new string[] { }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Equal("Some parameter", helpText);
            Assert.DoesNotContain("Allowed values:", helpText);
        }

        [Fact]
        public void OrderedParameter_MultipleAllowedValues_CommaSeparated()
        {
            // Arrange
            var attribute = new CommandParameterOrderedAttribute("level", "Compression level")
            {
                AllowedValues = new[] { "none", "low", "medium", "high", "maximum" }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Contains("none, low, medium, high, maximum", helpText);
        }

        [Fact]
        public void NamedAndOrderedParameters_SameFormatting_ForSingleAllowedValue()
        {
            // Arrange
            var orderedAttr = new CommandParameterOrderedAttribute("param1", "Test param")
            {
                AllowedValues = new[] { "only" }
            };

            var namedAttr = new CommandParameterNamedAttribute("param2", "Test param")
            {
                AllowedValues = new[] { "only" }
            };

            // Act
            var orderedHelp = orderedAttr.GetValueDescription();
            var namedHelp = namedAttr.GetValueDescription();

            // Assert - Both should format single value consistently
            Assert.Contains("(Allowed values: only)", orderedHelp);
            Assert.Contains("(Allowed values: only)", namedHelp);
        }

        [Fact]
        public void OrderedParameter_GetValueDescription_PreservesOriginalDescription()
        {
            // Arrange
            var description = "This is the original description with special chars: @#$%";
            var attribute = new CommandParameterOrderedAttribute("test", description)
            {
                AllowedValues = new[] { "a", "b" }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert - Original description should be preserved
            Assert.StartsWith(description, helpText);
            Assert.Contains("@#$%", helpText);
        }

        [Fact]
        public void OrderedParameter_WithAutoSetDefault_HelpTextStillShowsAllAllowedValues()
        {
            // Arrange - AllowedValues auto-sets DefaultValue to first item
            var attribute = new CommandParameterOrderedAttribute("option", "Choose option")
            {
                AllowedValues = new[] { "first", "second", "third" }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert - Should show ALL allowed values, not just the default
            Assert.Contains("first", helpText);
            Assert.Contains("second", helpText);
            Assert.Contains("third", helpText);
            Assert.Contains("first, second, third", helpText);
        }

        // ===== NEW TESTS FOR SUFFIX PARAMETER =====

        [Fact]
        public void SuffixParameter_WithAllowedValues_ShowsInHelpText()
        {
            // Arrange
            var attribute = new CommandParameterSuffixAttribute("extensions", "File extensions to process")
            {
                AllowedValues = new[] { ".txt", ".json", ".xml" }
            };

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Contains("File extensions to process", helpText);
            Assert.Contains("Allowed values:", helpText);
            Assert.Contains(".txt", helpText);
            Assert.Contains(".json", helpText);
            Assert.Contains(".xml", helpText);
        }

        [Fact]
        public void SuffixParameter_WithoutAllowedValues_ShowsOnlyDescription()
        {
            // Arrange
            var attribute = new CommandParameterSuffixAttribute("files", "Files to process");

            // Act
            var helpText = attribute.GetValueDescription();

            // Assert
            Assert.Equal("Files to process", helpText);
            Assert.DoesNotContain("Allowed values:", helpText);
        }

        [Fact]
        public void AllParameterTypes_HaveConsistentHelpFormat()
        {
            // Arrange
            var orderedAttr = new CommandParameterOrderedAttribute("param1", "Description")
            {
                AllowedValues = new[] { "a", "b" }
            };

            var namedAttr = new CommandParameterNamedAttribute("param2", "Description")
            {
                AllowedValues = new[] { "x", "y" }
            };

            var suffixAttr = new CommandParameterSuffixAttribute("param3", "Description")
            {
                AllowedValues = new[] { "1", "2" }
            };

            // Act
            var orderedHelp = orderedAttr.GetValueDescription();
            var namedHelp = namedAttr.GetValueDescription();
            var suffixHelp = suffixAttr.GetValueDescription();

            // Assert - All three should have same format
            Assert.Contains("(Allowed values:", orderedHelp);
            Assert.Contains("(Allowed values:", namedHelp);
            Assert.Contains("(Allowed values:", suffixHelp);
            
            Assert.EndsWith(")", orderedHelp);
            Assert.EndsWith(")", namedHelp);
            Assert.EndsWith(")", suffixHelp);
        }

        [Fact]
        public void SuffixParameter_WithAllowedValues_ValidatesDefaultValue()
        {
            // Arrange
            var attribute = new CommandParameterSuffixAttribute("format", "Output format")
            {
                AllowedValues = new[] { "json", "xml", "csv" }
            };

            // Act & Assert - Invalid default should throw
            var exception = Assert.Throws<ArgumentException>(() => attribute.DefaultValue = "yaml");
            Assert.Contains("Default value 'yaml' is not in the allowed values list", exception.Message);
            Assert.Contains("json, xml, csv", exception.Message);
        }

        [Fact]
        public void SuffixParameter_WithAllowedValues_AutoSetsDefaultToFirst()
        {
            // Arrange & Act
            var attribute = new CommandParameterSuffixAttribute("level", "Priority level")
            {
                AllowedValues = new[] { "low", "medium", "high" }
            };

            // Assert
            Assert.Equal("low", attribute.DefaultValue);
        }

        [Fact]
        public void SuffixParameter_ToString_IncludesAllowedValues()
        {
            // Arrange
            var attribute = new CommandParameterSuffixAttribute("type", "Resource type")
            {
                AllowedValues = new[] { "file", "directory", "link" }
            };

            // Act
            var fullHelpText = attribute.ToString();

            // Assert
            Assert.Contains("Allowed values:", fullHelpText);
            Assert.Contains("file, directory, link", fullHelpText);
        }
    }
}
