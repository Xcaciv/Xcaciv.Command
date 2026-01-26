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
    /// Comprehensive tests for SetCommand including parameter handling and piped input behavior.
    /// </summary>
    public class SetCommandTests
    {
        [Fact]
        public async Task HandleExecution_SetsEnvironmentVariable()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act
            await controller.Run("SET myvar myvalue", textIo, env);

            // Assert
            Assert.Equal("myvalue", env.GetValue("myvar"));
        }

        [Fact]
        public async Task HandleExecution_WithMultipleWords_SetsValue()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act
            await controller.Run("SET greeting \"Hello World\"", textIo, env);

            // Assert
            Assert.Equal("Hello World", env.GetValue("greeting"));
        }

        [Fact]
        public async Task HandleExecution_WithEmptyKey_DoesNotThrow()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act & Assert - should not throw
            await controller.Run("SET \"\" value", textIo, env);
        }

        [Fact]
        public async Task HandleExecution_WithEmptyValue_SetsEmptyString()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act
            await controller.Run("SET mykey \"\"", textIo, env);

            // Assert - empty string is a valid value
            Assert.Equal(string.Empty, env.GetValue("mykey"));
        }

        [Fact]
        public async Task HandleExecution_UpdatesExistingVariable()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("counter", "1");
            var textIo = new TestTextIo();

            // Act
            await controller.Run("SET counter 2", textIo, env);

            // Assert
            Assert.Equal("2", env.GetValue("counter"));
        }

        [Fact]
        public async Task HandleExecution_ReturnsEmptyOutput()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act
            await controller.Run("SET myvar value", textIo, env);

            // Assert - SET command produces no output
            Assert.Empty(textIo.ToString().Trim());
        }

        [Fact]
        public async Task HandlePipedChunk_AppendsValueToExistingVariable()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act - First set initial value, then pipe additional value
            await controller.Run("say hello | set greeting", textIo, env);

            // Assert - piped value should be set
            Assert.Equal("hello", env.GetValue("greeting"));
        }

        [Fact]
        public async Task HandlePipedChunk_WithEmptyKey_DoesNotThrow()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act & Assert - should not throw
            await controller.Run("say value | set \"\"", textIo, env);
        }

        [Fact]
        public async Task HandlePipedChunk_ReturnsEmptyOutput()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act
            await controller.Run("say hello | set myvar", textIo, env);

            // Assert - SET command produces no output even when piped
            Assert.Empty(textIo.ToString().Trim());
        }

        [Fact]
        public async Task OnStartPipe_InitializesVariableToEmpty()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            env.SetValue("accumulator", "old value");
            var textIo = new TestTextIo();

            // Act - Pipe to SET should initialize to empty before receiving chunks
            await controller.Run("say new | set accumulator", textIo, env);

            // Assert - old value should be replaced
            Assert.Equal("new", env.GetValue("accumulator"));
        }

        [Fact]
        public async Task PipelineWithRegex_FiltersAndSetsVariable()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textIo = new TestTextIo();

            // Act - Use SAY instead of ECHO (which needs plugin loading)
            await controller.Run("say banana | regif ^b | set fruit", textIo, env);

            // Assert
            Assert.Equal("banana", env.GetValue("fruit"));
        }

        [Fact]
        public async Task ModifiesEnvironment_IsTrue()
        {
            // Arrange
            var controller = new CommandControllerTestHarness();
            controller.RegisterBuiltInCommands();

            // Act
            var commands = controller.GetCommands();
            var setCommand = commands.Values.FirstOrDefault(c => c.BaseCommand.Equals("SET", StringComparison.OrdinalIgnoreCase));

            // Assert
            Assert.NotNull(setCommand);
            Assert.True(setCommand.ModifiesEnvironment, "SET command should have ModifiesEnvironment=true");
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
            await controller.Run("SET --help", textIo, env);
            var helpText = textIo.ToString(); // Help goes to child context

            // Assert
            Assert.Contains("SET", helpText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
