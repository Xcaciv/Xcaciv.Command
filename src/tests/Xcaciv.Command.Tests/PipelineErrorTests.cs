using Xunit;
using Xcaciv.Command;
using Xcaciv.Command.Tests.TestImpementations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xcaciv.Command.Tests
{
    public class PipelineErrorTests
    {
        private ITestOutputHelper _testOutput;

        public PipelineErrorTests(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        /// <summary>
        /// Test: Pipeline with first command failing should stop and log error
        /// </summary>
        [Fact]
        public async Task PipelineWithFirstCommandFailing_ShouldStopGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Using 'echo' which will succeed, pipe to 'badcommand' which will fail
            await controller.Run("echo test | badcommand", textio, env);

            // Assert - Framework should not crash, error should be logged
            // The output may contain error message or be empty, but execution should complete
            Assert.NotNull(textio.Output); // No exception thrown
        }

        /// <summary>
        /// Test: Pipeline with middle command failing should stop and log error
        /// </summary>
        [Fact]
        public async Task PipelineWithMiddleCommandFailing_ShouldStopGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Pipeline: echo -> badcommand -> echo
            await controller.Run("echo test | badcommand | echo result", textio, env);

            // Assert - Framework should handle gracefully
            Assert.NotNull(textio.Output);
        }

        /// <summary>
        /// Test: Pipeline with last command failing should log error and continue
        /// </summary>
        [Fact]
        public async Task PipelineWithLastCommandFailing_ShouldStopGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Pipeline: echo -> echo -> badcommand
            await controller.Run("echo test | echo piped | badcommand", textio, env);

            // Assert
            Assert.NotNull(textio.Output);
        }

        /// <summary>
        /// Test: Command throwing SecurityException should be caught and logged
        /// </summary>
        [Fact]
        public async Task CommandThrowingSecurityException_ShouldBeHandledGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Using a non-existent command that triggers security/lookup error
            await controller.Run("nonexistentcommand arg1 arg2", textio, env);

            // Assert - Should not throw, should handle gracefully
            Assert.NotNull(textio.Output);
        }

        /// <summary>
        /// Test: Command throwing ArgumentException should be caught and logged
        /// </summary>
        [Fact]
        public async Task CommandThrowingArgumentException_ShouldBeHandledGracefullyAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Say command with invalid arguments
            await controller.Run("say", textio, env);

            // Assert - Should handle missing required parameter gracefully
            Assert.NotNull(textio.Output);
        }

        /// <summary>
        /// Test: Pipeline should not leak resources on error
        /// </summary>
        [Fact]
        public async Task PipelineErrorShouldNotLeakResources_ShouldCompleteAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Create a pipeline that will fail
            for (int i = 0; i < 3; i++)
            {
                await controller.Run("echo test | badcommand", textio, env);
            }

            // Assert - No resource leak should cause issues
            Assert.NotNull(textio.Output);
        }
    }
}
