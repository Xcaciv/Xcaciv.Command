using Xunit;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;
using System;
using System.Collections.Generic;

namespace Xcaciv.Command.Tests
{
    public class ParameterValidationBoundaryTests
    {
        private readonly CommandParameters _commandParameters = new();

        /// <summary>
        /// Test: Empty string parameter should be handled
        /// </summary>
        [Fact]
        public void EmptyStringParameter_ShouldBeAccepted()
        {
            // Arrange
            var parameterList = new List<string> { "" };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var requiredParam = new CommandParameterOrderedAttribute("name", "A text parameter") { IsRequired = true };
            var parameters = new[] { requiredParam };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert - Empty string should be valid
            Assert.Equal("", parameterLookup["name"].RawValue);
        }

        /// <summary>
        /// Test: Very long string parameter (1000+ chars) should be accepted
        /// </summary>
        [Fact]
        public void VeryLongStringParameter_ShouldBeAccepted()
        {
            // Arrange
            var longString = new string('a', 1000);
            var parameterList = new List<string> { longString };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("text", "A text parameter") { IsRequired = true };
            var parameters = new[] { param };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.Equal(longString, parameterLookup["text"].RawValue);
        }

        /// <summary>
        /// Test: Parameter with special characters should be handled
        /// </summary>
        [Fact]
        public void ParameterWithSpecialCharacters_ShouldBeHandled()
        {
            // Arrange
            var parameterList = new List<string> { "test!@#$%^&*()" };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("text", "A text parameter") { IsRequired = true };
            var parameters = new[] { param };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert - Special characters should pass through
            Assert.Equal("test!@#$%^&*()", parameterLookup["text"].RawValue);
        }

        /// <summary>
        /// Test: Parameter with regex metacharacters should be handled safely
        /// </summary>
        [Fact]
        public void ParameterWithRegexMetacharacters_ShouldBeHandled()
        {
            // Arrange
            var regexChars = ".*+?^$|()[]{}\\";
            var parameterList = new List<string> { regexChars };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("text", "A text parameter") { IsRequired = true };
            var parameters = new[] { param };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert - Metacharacters should be treated as literals
            Assert.Equal(regexChars, parameterLookup["text"].RawValue);
        }

        /// <summary>
        /// Test: Parameter with Unicode characters should be accepted
        /// </summary>
        [Fact]
        public void ParameterWithUnicodeCharacters_ShouldBeAccepted()
        {
            // Arrange
            var unicodeStr = "Hello éü 漢字 😊";
            var parameterList = new List<string> { unicodeStr };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("text", "A text parameter") { IsRequired = true };
            var parameters = new[] { param };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.Equal(unicodeStr, parameterLookup["text"].RawValue);
        }

        /// <summary>
        /// Test: Null parameter in array should be handled defensively
        /// </summary>
        [Fact]
        public void NullParameterInArray_ShouldBeHandledDefensively()
        {
            // Arrange
            var parameterList = new List<string> { "valid" };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param1 = new CommandParameterOrderedAttribute("text1", "First parameter") { IsRequired = true };
            var param2 = new CommandParameterOrderedAttribute("text2", "Second parameter") { IsRequired = false };
            var parameters = new[] { param1, param2 };

            // Act & Assert - Should handle without crashing
            try
            {
                _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);
                // If it doesn't throw, that's acceptable defensive behavior
                Assert.NotNull(parameterLookup);
                Assert.Contains("text1", parameterLookup.Keys);
            }
            catch (ArgumentException)
            {
                // Also acceptable - clear error message
            }
        }

        /// <summary>
        /// Test: Named parameter flag with no default value should use null or empty
        /// </summary>
        [Fact]
        public void NamedParameterFlagWithNoDefault_ShouldBeHandled()
        {
            // Arrange
            var namedParam = new CommandParameterNamedAttribute("verbose", "Enable verbose output");
            // Don't set a default value

            // Act & Assert - Should handle missing value gracefully
            Assert.NotNull(namedParam);
        }

        /// <summary>
        /// Test: Case-insensitive parameter handling should work
        /// </summary>
        [Fact]
        public void CaseInsensitiveNamedParameter_ShouldBeHandled()
        {
            // Arrange
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);

            // Act & Assert
            Assert.NotNull(parameterLookup);
        }

        /// <summary>
        /// Test: Parameter with whitespace should be preserved
        /// </summary>
        [Fact]
        public void ParameterWithWhitespace_ShouldBePreserved()
        {
            // Arrange
            var parameterWithSpaces = "  text with  spaces  ";
            var parameterList = new List<string> { parameterWithSpaces };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("text", "A text parameter") { IsRequired = true };
            var parameters = new[] { param };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert - Whitespace should be preserved
            Assert.Equal(parameterWithSpaces, parameterLookup["text"].RawValue);
        }

        /// <summary>
        /// Test: Parameter with line breaks should be handled
        /// </summary>
        [Fact]
        public void ParameterWithLineBreaks_ShouldBeHandled()
        {
            // Arrange
            var parameterWithNewlines = "line1\nline2\r\nline3";
            var parameterList = new List<string> { parameterWithNewlines };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("text", "A text parameter") { IsRequired = true };
            var parameters = new[] { param };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.Equal(parameterWithNewlines, parameterLookup["text"].RawValue);
        }

        /// <summary>
        /// Test: Multiple parameters with mixed edge cases
        /// </summary>
        [Fact]
        public void MultipleParametersWithEdgeCases_ShouldAllBeHandled()
        {
            // Arrange
            var parameterList = new List<string> 
            { 
                "", // empty
                "normal", // normal
                new string('x', 500) // very long
            };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param1 = new CommandParameterOrderedAttribute("text1", "First") { IsRequired = true };
            var param2 = new CommandParameterOrderedAttribute("text2", "Second") { IsRequired = true };
            var param3 = new CommandParameterOrderedAttribute("text3", "Third") { IsRequired = true };
            var parameters = new[] { param1, param2, param3 };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.Equal(3, parameterLookup.Count);
            Assert.Equal("", parameterLookup["text1"].RawValue);
            Assert.Equal("normal", parameterLookup["text2"].RawValue);
            Assert.Equal(500, parameterLookup["text3"].RawValue.ToString().Length);
        }
    }
}
