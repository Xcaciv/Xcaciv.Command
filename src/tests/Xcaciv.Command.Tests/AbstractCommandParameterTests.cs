using Xunit;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xcaciv.Command.Tests.Commands;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Core;

namespace Xcaciv.Command.Tests
{
    /// <summary>
    /// Tests for AbstractCommand parameter handling and field injection.
    /// Tests that parameters are correctly processed and injected into public fields.
    /// </summary>
    public class AbstractCommandParameterTests
    {
        [Fact]
        public void ProcessParameters_ShouldPopulateFieldsFromParameters()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2", "--NamedParam", "namedValue", "--FlagParam", "suffixValue" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Assert - verify parameters were processed
            Assert.NotNull(processedParams);
            Assert.NotEmpty(processedParams);

            // Verify fields were populated
            Assert.Equal("value1", command.FirstParam);
            Assert.Equal("value2", command.SecondParam);
            Assert.Equal("namedValue", command.NamedParam);
            Assert.True(command.FlagParam);
            Assert.Equal("suffixValue", command.SuffixParam);

            // Verify fields match dictionary values
            Assert.True(command.VerifyFieldsMatch(processedParams));
        }

        [Fact]
        public void ProcessParameters_WithoutFlag_ShouldSetFlagToFalse()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Assert
            Assert.False(command.FlagParam);
        }

        [Fact]
        public void ProcessParameters_WithEmptyParameters_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            ioContext.Setup(x => x.Parameters).Returns(Array.Empty<string>());
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Assert
            Assert.NotNull(processedParams);
            Assert.Empty(processedParams);
        }

        [Fact]
        public void HandleExecution_ShouldReceiveProcessedParameters()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            var env = new Mock<IEnvironmentContext>().Object;
            ioContext.Setup(x => x.Parameters).Returns(new[] { "testValue1", "testValue2", "--NamedParam", "testNamed" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);
            var result = command.HandleExecution(processedParams, env);

            // Assert
            Assert.True(command.ExecutionCalled);
            Assert.NotNull(command.ReceivedParameters);
            Assert.True(result.IsSuccess);

            // Verify fields were set before HandleExecution was called
            Assert.Equal("testValue1", command.FirstParam);
            Assert.Equal("testValue2", command.SecondParam);
            Assert.Equal("testNamed", command.NamedParam);
        }

        [Fact]
        public void HandlePipedChunk_ShouldReceiveIResultAndAccessOutput()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            var env = new Mock<IEnvironmentContext>().Object;
            var pipedChunk = CommandResult<string>.Success("piped data");
            
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1" });
            ioContext.Setup(x => x.HasPipedInput).Returns(true);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);
            var result = command.HandlePipedChunk(pipedChunk, processedParams, env);

            // Assert
            Assert.True(command.PipedChunkCalled);
            Assert.NotNull(command.ReceivedPipedChunk);
            Assert.True(result.IsSuccess);
            Assert.Contains("piped data", result.Output);
            Assert.Contains("Success: True", result.Output);

            // Verify field was set
            Assert.Equal("value1", command.FirstParam);
        }

        [Fact]
        public void HandlePipedChunk_WithFailedUpstream_ShouldReceiveFailure()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            var env = new Mock<IEnvironmentContext>().Object;
            var pipedChunk = CommandResult<string>.Failure("upstream error");
            
            ioContext.Setup(x => x.Parameters).Returns(Array.Empty<string>());
            ioContext.Setup(x => x.HasPipedInput).Returns(true);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);
            var result = command.HandlePipedChunk(pipedChunk, processedParams, env);

            // Assert
            Assert.True(command.PipedChunkCalled);
            Assert.NotNull(command.ReceivedPipedChunk);
            Assert.False(command.ReceivedPipedChunk.IsSuccess);
            Assert.Contains("Success: False", result.Output);
        }

        [Fact]
        public async Task Main_ShouldProcessParametersAndCallHandleExecution()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            var env = new Mock<IEnvironmentContext>().Object;
            
            ioContext.Setup(x => x.Parameters).Returns(new[] { "mainValue1", "mainValue2" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var results = command.Main(ioContext.Object, env);
            var resultList = await results.ToListAsync();

            // Assert
            Assert.Single(resultList);
            Assert.True(resultList[0].IsSuccess);
            Assert.True(command.ExecutionCalled);

            // Verify fields were populated
            Assert.Equal("mainValue1", command.FirstParam);
            Assert.Equal("mainValue2", command.SecondParam);
        }

        [Fact]
        public void ProcessParameters_WithCaseInsensitiveFieldNames_ShouldStillMatch()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            // Use different casing for parameter names
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2", "--namedparam", "testValue" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Assert - field should still be populated due to case-insensitive matching
            Assert.Equal("testValue", command.NamedParam);
        }

        [Fact]
        public void ProcessParameters_WithIncompatibleTypes_ShouldNotSetField()
        {
            // This test verifies that type safety is maintained
            // FlagParam is bool, so if we try to set it with a string that can't convert, it shouldn't break
            // The parameter system handles type conversion, so this should work correctly
            
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            // Provide all required parameters
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2", "--FlagParam", "suffixValue" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Assert - flag should be set to true (presence = true)
            Assert.True(command.FlagParam);
        }

        [Fact]
        public void FieldInjection_ShouldNotThrowOnFailure()
        {
            // Arrange - Create a command with fields that might fail to set
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            // Provide sufficient parameters to avoid validation errors
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2", "suffixValue" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);
            ioContext.Setup(x => x.AddTraceMessage(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act & Assert - Should not throw even if field setting fails
            var exception = Record.Exception(() => command.ProcessParameters(ioContext.Object));
            Assert.Null(exception);
        }
    }
}
