using System;
using Xunit;
using Xcaciv.Command.Interface.Attributes;

namespace Xcaciv.Command.Tests.Attributes
{
    public class AllowedValuesValidationTests
    {
        [Fact]
        public void CommandParameterNamed_WithAllowedValues_AutoSetsDefaultToFirst()
        {
            // Arrange & Act
            var attribute = new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                AllowedValues = new[] { "quiet", "normal", "detailed" }
            };

            // Assert
            Assert.Equal("quiet", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterNamed_WithValidDefaultAndAllowedValues_DoesNotThrow()
        {
            // Arrange & Act
            var attribute = new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                AllowedValues = new[] { "quiet", "normal", "detailed" },
                DefaultValue = "normal"
            };

            // Assert
            Assert.Equal("normal", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterNamed_WithInvalidDefaultAndAllowedValues_Throws()
        {
            // Arrange
            var attribute = new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                AllowedValues = new[] { "quiet", "normal", "detailed" }
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => attribute.DefaultValue = "invalid");
            Assert.Contains("Default value 'invalid' is not in the allowed values list", exception.Message);
            Assert.Contains("quiet, normal, detailed", exception.Message);
        }

        [Fact]
        public void CommandParameterNamed_WithDefaultSetBeforeAllowedValues_ValidatesCorrectly()
        {
            Assert.Throws<ArgumentException>(() => new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                DefaultValue = "normal",
                AllowedValues = new[] { "quiet", "detailed" }
            });
        }

        [Fact]
        public void CommandParameterNamed_CaseInsensitiveValidation_Succeeds()
        {
            // Arrange & Act
            var attribute = new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                AllowedValues = new[] { "Quiet", "Normal", "Detailed" },
                DefaultValue = "normal"  // lowercase
            };

            // Assert - should not throw, case-insensitive comparison
            Assert.Equal("normal", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterOrdered_WithAllowedValues_AutoSetsDefaultToFirst()
        {
            // Arrange & Act
            var attribute = new CommandParameterOrderedAttribute("mode", "Operation mode")
            {
                AllowedValues = new[] { "read", "write", "append" }
            };

            // Assert
            Assert.Equal("read", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterOrdered_WithValidDefaultAndAllowedValues_DoesNotThrow()
        {
            // Arrange & Act
            var attribute = new CommandParameterOrderedAttribute("mode", "Operation mode")
            {
                AllowedValues = new[] { "read", "write", "append" },
                DefaultValue = "write"
            };

            // Assert
            Assert.Equal("write", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterOrdered_WithInvalidDefaultAndAllowedValues_Throws()
        {
            // Arrange
            var attribute = new CommandParameterOrderedAttribute("mode", "Operation mode")
            {
                AllowedValues = new[] { "read", "write", "append" }
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => attribute.DefaultValue = "delete");
            Assert.Contains("Default value 'delete' is not in the allowed values list", exception.Message);
            Assert.Contains("read, write, append", exception.Message);
        }

        [Fact]
        public void CommandParameterNamed_EmptyAllowedValues_DoesNotAutoSetDefault()
        {
            // Arrange & Act
            var attribute = new CommandParameterNamedAttribute("param", "Test parameter")
            {
                AllowedValues = new string[] { }
            };

            // Assert
            Assert.Equal("", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterNamed_NullAllowedValues_DoesNotAutoSetDefault()
        {
            // Arrange & Act
            var attribute = new CommandParameterNamedAttribute("param", "Test parameter")
            {
                AllowedValues = null!
            };

            // Assert
            Assert.Equal("", attribute.DefaultValue);
        }

        [Fact]
        public void CommandParameterNamed_WithExistingDefault_DoesNotOverrideWhenSettingAllowedValues()
        {
            var attribute = new CommandParameterNamedAttribute("verbosity", "Log verbosity level")
            {
                AllowedValues = new[] { "quiet", "normal", "detailed" },
                DefaultValue = "detailed"
            };

            Assert.Equal("detailed", attribute.DefaultValue);
        }
    }
}
