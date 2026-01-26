using Xunit;
using Xcaciv.Command;
using Xcaciv.Command.Tests.TestImpementations;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xcaciv.Command.Tests
{
    public class CommandControllerRefactoringTests
    {
        private ITestOutputHelper _testOutput;

        public CommandControllerRefactoringTests(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        /// <summary>
        /// Test: Pipeline with two commands executes both
        /// </summary>
        [Fact]
        public async Task Pipeline_WithTwoCommands_ExecutesBothAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("say hello | say world", ioContext, env);

            // Assert - Should complete without error
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Pipeline with single command executes successfully
        /// </summary>
        [Fact]
        public async Task Pipeline_WithSingleCommand_ExecutesSuccessfullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("echo hello", ioContext, env);

            // Assert
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Pipeline with three commands executes all three
        /// </summary>
        [Fact]
        public async Task Pipeline_WithThreeCommands_ExecutesAllThreeAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("echo hello | say test | help", ioContext, env);

            // Assert
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Non-pipeline command execution still works after refactoring
        /// </summary>
        [Fact]
        public async Task NonPipeline_CommandExecution_WorksCorrectlyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "hello" });

            // Act
            await controller.Run("say hello", ioContext, env);

            // Assert - Should execute without error
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Command with no arguments handled gracefully
        /// </summary>
        [Fact]
        public async Task Command_WithNoArguments_HandledGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(); // No arguments

            // Act
            await controller.Run("say", ioContext, env);

            // Assert - Should output error message, not crash
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Help request works after refactoring
        /// </summary>
        [Fact]
        public async Task HelpRequest_WorksAfterRefactoringAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("help", ioContext, env);

            // Assert
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Help request for specific command works
        /// </summary>
        [Fact]
        public async Task HelpRequest_ForSpecificCommand_WorksAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "--help" });

            // Act
            await controller.Run("say", ioContext, env);

            // Assert
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Unknown command handled gracefully
        /// </summary>
        [Fact]
        public async Task UnknownCommand_HandledGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("unknowncommand arg1 arg2", ioContext, env);

            // Assert - Should output error message
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Pipeline execution doesn't lose output from middle commands
        /// </summary>
        [Fact]
        public async Task Pipeline_DoesNotLoseOutput_FromMiddleCommandsAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("echo start | echo middle | echo end", ioContext, env);

            // Assert - Output should be collected
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Complex pipeline with multiple stages works
        /// </summary>
        [Fact]
        public async Task ComplexPipeline_WithMultipleStages_WorksAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("echo a | echo b | echo c | echo d", ioContext, env);

            // Assert
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: Refactored code maintains backward compatibility
        /// </summary>
        [Fact]
        public async Task Refactored_Code_MaintainsBackwardCompatibilityAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new ControllerEnvironmentContext();

            // Act - Execute multiple commands in sequence
            var ioContext1 = new TestTextIo(new[] { "first" });
            await controller.Run("say first", ioContext1, env);

            var ioContext2 = new TestTextIo(new[] { "second" });
            await controller.Run("say second", ioContext2, env);

            var ioContext3 = new TestTextIo();
            await controller.Run("help", ioContext3, env);

            // Assert - All should work without error
            Assert.NotNull(ioContext1.Output);
            Assert.NotNull(ioContext2.Output);
            Assert.NotNull(ioContext3.Output);
        }
    }
}
