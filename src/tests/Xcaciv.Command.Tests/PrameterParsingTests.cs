using Xunit;
using Moq;
using Xcaciv.Command.Commands;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using System;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests
{
    public class PrameterParsingTests
    {
        [Fact]
        public void HandleExecution_ShouldReturnCorrectResult()
        {
            // Arrange
            var parameters = new string[] { "param1", "-flag", "--name", "John", "suffix" };
            var env = new Mock<IEnvironmentContext>().Object;
            var command = new Commands.ParameterTestCommand(
                [new CommandParameterOrderedAttribute("first", "a param")], 
                [new CommandFlagAttribute("flag", "a flag value")],
                [new CommandParameterNamedAttribute("--name", "a name value")], 
                [new CommandParameterSuffixAttribute("unnamed", "a unnamed value")]);

            // Act
            var processedParams = command.ProcessParameters(parameters);
            var result = command.HandleExecution(processedParams, env);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("first = param1", result.Output);
            Assert.Contains("flag = true", result.Output);  // Changed to lowercase true from True
            Assert.Contains("name = John", result.Output);
            Assert.Contains("unnamed = suffix", result.Output);
        }

        [Fact]
        public void HandlePipedChunk_ShouldThrowNotImplementedException()
        {
            // Arrange
            var pipedChunk = CommandResult<string>.Success("piped chunk");
            var parameters = new string[] { };
            var env = new Mock<IEnvironmentContext>().Object;
            var command = new Commands.ParameterTestCommand([], [], [], []);

            // Act
            var processedParams = command.ProcessParameters(parameters);

            // Assert
            Assert.Throws<NotImplementedException>(() => command.HandlePipedChunk(pipedChunk, processedParams, env));
        }
    }
}
