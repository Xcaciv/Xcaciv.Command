using Xunit;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;
using System;
using System.Collections.Generic;

namespace Xcaciv.Command.Tests
{
    public class ParameterBoundsTests
    {
        private readonly CommandParameters _commandParameters = new();

        /// <summary>
        /// Test: Empty parameter list with required ordered parameter should throw
        /// </summary>
        [Fact]
        public void ProcessOrderedParameters_EmptyListWithRequiredParameter_ShouldThrow()
        {
            // Arrange
            var parameterList = new List<string>();
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var requiredParam = new CommandParameterOrderedAttribute("name", "A required parameter") { IsRequired = true };
            var parameters = new[] { requiredParam };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters)
            );
        }

        /// <summary>
        /// Test: Empty parameter list with optional ordered parameter with default value should use default
        /// </summary>
        [Fact]
        public void ProcessOrderedParameters_EmptyListWithOptionalParameter_ShouldUseDefault()
        {
            // Arrange
            var parameterList = new List<string>();
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var optionalParam = new CommandParameterOrderedAttribute("name", "An optional parameter") 
            { 
                IsRequired = false, 
                DefaultValue = "defaultValue" 
            };
            var parameters = new[] { optionalParam };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.True(parameterLookup.ContainsKey("name"));
            Assert.Equal("defaultValue", parameterLookup["name"].RawValue);
        }

        /// <summary>
        /// Test: Empty parameter list with optional ordered parameter without default should skip
        /// </summary>
        [Fact]
        public void ProcessOrderedParameters_EmptyListWithOptionalParameterNoDefault_ShouldSkip()
        {
            // Arrange
            var parameterList = new List<string>();
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var optionalParam = new CommandParameterOrderedAttribute("name", "An optional parameter") 
            { 
                IsRequired = false, 
                DefaultValue = "" 
            };
            var parameters = new[] { optionalParam };

            // Act
            _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters);

            // Assert - should not add parameter if not required and no default
            Assert.False(parameterLookup.ContainsKey("name"));
        }

        /// <summary>
        /// Test: Single parameter when two ordered parameters required should throw
        /// </summary>
        [Fact]
        public void ProcessOrderedParameters_SingleParameterTwoRequired_ShouldThrow()
        {
            // Arrange
            var parameterList = new List<string> { "value1" };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param1 = new CommandParameterOrderedAttribute("first", "First parameter") { IsRequired = true };
            var param2 = new CommandParameterOrderedAttribute("second", "Second parameter") { IsRequired = true };
            var parameters = new[] { param1, param2 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters)
            );
        }

        /// <summary>
        /// Test: Empty parameter list with required suffix parameter should throw
        /// </summary>
        [Fact]
        public void ProcessSuffixParameters_EmptyListWithRequiredParameter_ShouldThrow()
        {
            // Arrange
            var parameterList = new List<string>();
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var requiredParam = new CommandParameterSuffixAttribute("trailing", "A required parameter") { IsRequired = true };
            var parameters = new[] { requiredParam };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _commandParameters.ProcessSuffixParameters(parameterList, parameterLookup, parameters)
            );
        }

        /// <summary>
        /// Test: Empty parameter list with optional suffix parameter with default value should use default
        /// </summary>
        [Fact]
        public void ProcessSuffixParameters_EmptyListWithOptionalParameter_ShouldUseDefault()
        {
            // Arrange
            var parameterList = new List<string>();
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var optionalParam = new CommandParameterSuffixAttribute("trailing", "An optional parameter") 
            { 
                IsRequired = false, 
                DefaultValue = "defaultValue" 
            };
            var parameters = new[] { optionalParam };

            // Act
            _commandParameters.ProcessSuffixParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.True(parameterLookup.ContainsKey("trailing"));
            Assert.Equal("defaultValue", parameterLookup["trailing"].RawValue);
        }

        /// <summary>
        /// Test: Empty parameter list with optional suffix parameter without default should skip
        /// </summary>
        [Fact]
        public void ProcessSuffixParameters_EmptyListWithOptionalParameterNoDefault_ShouldSkip()
        {
            // Arrange
            var parameterList = new List<string>();
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var optionalParam = new CommandParameterSuffixAttribute("trailing", "An optional parameter") 
            { 
                IsRequired = false, 
                DefaultValue = "" 
            };
            var parameters = new[] { optionalParam };

            // Act
            _commandParameters.ProcessSuffixParameters(parameterList, parameterLookup, parameters);

            // Assert - should not add parameter if not required and no default
            Assert.False(parameterLookup.ContainsKey("trailing"));
        }

        /// <summary>
        /// Test: Null parameter list should throw (defensive programming)
        /// </summary>
        [Fact]
        public void ProcessOrderedParameters_NullParameterList_ShouldThrow()
        {
            // Arrange
            List<string> parameterList = null!;
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var param = new CommandParameterOrderedAttribute("name", "A parameter");
            var parameters = new[] { param };

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
                _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters)
            );
        }

        /// <summary>
        /// Test: Named parameter flag encountered in ordered parameters with no default
        /// </summary>
        [Fact]
        public void ProcessOrderedParameters_NamedParameterFlagWithNoDefault_ShouldThrow()
        {
            // Arrange
            var parameterList = new List<string> { "--name" };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var requiredParam = new CommandParameterOrderedAttribute("value", "Required parameter") 
            { 
                IsRequired = true, 
                DefaultValue = "" 
            };
            var parameters = new[] { requiredParam };

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _commandParameters.ProcessOrderedParameters(parameterList, parameterLookup, parameters)
            );
        }

        /// <summary>
        /// Test: Named parameter flag encountered in suffix parameters
        /// </summary>
        [Fact]
        public void ProcessSuffixParameters_NamedParameterFlag_ShouldUseDefaultIfAvailable()
        {
            // Arrange
            var parameterList = new List<string> { "--name" };
            var parameterLookup = new Dictionary<string, IParameterValue>(StringComparer.OrdinalIgnoreCase);
            var optionalParam = new CommandParameterSuffixAttribute("trailing", "Optional parameter") 
            { 
                IsRequired = false, 
                DefaultValue = "defaultValue" 
            };
            var parameters = new[] { optionalParam };

            // Act
            _commandParameters.ProcessSuffixParameters(parameterList, parameterLookup, parameters);

            // Assert
            Assert.True(parameterLookup.ContainsKey("trailing"));
            Assert.Equal("defaultValue", parameterLookup["trailing"].RawValue);
        }
    }
}
