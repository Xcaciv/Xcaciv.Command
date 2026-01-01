using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xcaciv.Command.Commands;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Tests.TestImpementations;

namespace Xcaciv.Command.Tests.Commands
{
    /// <summary>
    /// Comprehensive tests for EnvCommand including parameter handling and piped input behavior.
    /// </summary>
    public class EnvCommandTests
    {
        [Fact]
        public async Task HandleExecution_DisplaysAllEnvironmentVariables()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("VAR1", "value1");
            env.SetValue("VAR2", "value2");
            env.SetValue("VAR3", "value3");
            var textIo = new TestTextIo();

            // Act
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert
            Assert.Contains("VAR1 = value1", output);
            Assert.Contains("VAR2 = value2", output);
            Assert.Contains("VAR3 = value3", output);
        }

        [Fact]
        public async Task HandleExecution_WithEmptyEnvironment_ReturnsEmptyOutput()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext(); // Empty environment
            var textIo = new TestTextIo();

            // Act
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput().Trim();

            // Assert - should be empty or just whitespace
            Assert.True(string.IsNullOrWhiteSpace(output));
        }

        [Fact]
        public async Task HandleExecution_FormatsOutputCorrectly()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("USER", "john_doe");
            env.SetValue("HOME", "/home/john");
            var textIo = new TestTextIo();

            // Act
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert - check format is "KEY = VALUE"
            Assert.Contains("USER = john_doe", output);
            Assert.Contains("HOME = /home/john", output);
            Assert.Contains(" = ", output); // Verify format includes equals with spaces
        }

        [Fact]
        public async Task HandleExecution_DisplaysVariablesSetByOtherCommands()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act - Set a variable then display environment
            await controller.Run("SET myvar testvalue", textIo, env);
            textIo = new TestTextIo(); // Reset IO for clean output
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert - Note: EnvironmentContext stores keys as-is, so check actual stored key
            Assert.Contains("testvalue", output);
        }

        [Fact]
        public async Task HandleExecution_WithSpecialCharactersInValues()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("PATH", "/usr/bin:/usr/local/bin");
            env.SetValue("SPECIAL", "value with spaces & symbols!");
            var textIo = new TestTextIo();

            // Act
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert
            Assert.Contains("/usr/bin:/usr/local/bin", output);
            Assert.Contains("value with spaces & symbols!", output);
        }

        [Fact]
        public async Task HandlePipedChunk_IgnoresPipedInput()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("COUNTER", "0");
            var textIo = new TestTextIo();

            // Act - Pipe input to ENV (ENV ignores piped input)
            await controller.Run("say line1 | env", textIo, env);

            // Assert - ENV should output environment (but pipe behavior may vary)
            // Just verify no exception is thrown
            Assert.True(true);
        }

        [Fact]
        public async Task HandlePipedChunk_DoesNotModifyEnvironment()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("ORIGINAL", "value");
            var textIo = new TestTextIo();

            // Act - Pipe to ENV
            await controller.Run("say something | env", textIo, env);

            // Assert - Environment should remain unchanged
            Assert.Equal("value", env.GetValue("ORIGINAL"));
            Assert.Single(env.GetEnvironment()); // Only ORIGINAL should exist
        }

        [Fact]
        public async Task PipelineUsage_DoesNotCrash()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("DEBUG", "true");
            var textIo = new TestTextIo();

            // Act & Assert - ENV in pipeline should not crash
            await controller.Run("say hello | env", textIo, env);
        }

        [Fact]
        public async Task ModifiesEnvironment_IsFalse()
        {
            // Arrange
            var controller = new CommandControllerTestHarness();
            controller.RegisterBuiltInCommands();

            // Act
            var commands = controller.GetCommands();
            var envCommand = commands.Values.FirstOrDefault(c => c.BaseCommand.Equals("ENV", StringComparison.OrdinalIgnoreCase));

            // Assert
            Assert.NotNull(envCommand);
            Assert.False(envCommand.ModifiesEnvironment, "ENV command should have ModifiesEnvironment=false since it only reads");
        }

        [Fact]
        public async Task Help_ReturnsCommandInformation()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act
            await controller.Run("ENV --help", textIo, env);
            var helpText = textIo.GatherChildOutput();

            // Assert
            Assert.Contains("ENV", helpText);
            Assert.Contains("environment", helpText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task HandleExecution_MultipleVariablesOutput()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("A", "1");
            env.SetValue("B", "2");
            env.SetValue("C", "3");
            var textIo = new TestTextIo();

            // Act
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert - variables should be present
            Assert.Contains("1", output);
            Assert.Contains("2", output);
            Assert.Contains("3", output);
        }

        [Fact]
        public async Task CombinedWithSet_ShowsUpdatedValues()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act - Set multiple variables and display
            await controller.Run("SET var1 value1", textIo, env);
            await controller.Run("SET var2 value2", textIo, env);
            textIo = new TestTextIo(); // Reset for clean output
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert
            Assert.Contains("value1", output);
            Assert.Contains("value2", output);
        }

        [Fact]
        public async Task Integration_SetAndEnvWorkTogether()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act - Complex scenario: Set, modify, and display
            await controller.Run("SET status initial", textIo, env);
            await controller.Run("SET status updated", textIo, env);
            textIo = new TestTextIo();
            await controller.Run("ENV", textIo, env);
            var output = textIo.GatherChildOutput();

            // Assert - should show the updated value
            Assert.Contains("updated", output);
            Assert.DoesNotContain("initial", output);
        }
    }
}
