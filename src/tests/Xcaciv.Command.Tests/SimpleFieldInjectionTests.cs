using Xunit;
using Xunit.Abstractions;
using Moq;
using Xcaciv.Command.Tests.Commands;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Tests
{
    public class SimpleFieldInjectionTests
    {
        private readonly ITestOutputHelper _output;

        public SimpleFieldInjectionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test_OrderedParametersOnly()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Debug
            _output.WriteLine("Processed Parameters:");
            foreach (var kvp in processedParams)
            {
                _output.WriteLine($"  {kvp.Key} = '{kvp.Value.RawValue}'");
            }
            _output.WriteLine($"\nFirstParam field = '{command.FirstParam}'");
            _output.WriteLine($"SecondParam field = '{command.SecondParam}'");

            // Assert
            Assert.Equal("value1", command.FirstParam);
            Assert.Equal("value2", command.SecondParam);
        }

        [Fact]
        public void Test_NamedParameterOnly()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            ioContext.Setup(x => x.Parameters).Returns(new[] { "--NamedParam", "testValue" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Debug
            _output.WriteLine("Processed Parameters:");
            foreach (var kvp in processedParams)
            {
                _output.WriteLine($"  {kvp.Key} = '{kvp.Value.RawValue}'");
            }
            _output.WriteLine($"\nNamedParam field = '{command.NamedParam ?? "null"}'");

            // Assert
            Assert.Equal("testValue", command.NamedParam);
        }

        [Fact]
        public void Test_FlagOnly()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            ioContext.Setup(x => x.Parameters).Returns(new[] { "--FlagParam" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Debug
            _output.WriteLine("Processed Parameters:");
            foreach (var kvp in processedParams)
            {
                _output.WriteLine($"  {kvp.Key} = '{kvp.Value.RawValue}'");
            }
            _output.WriteLine($"\nFlagParam field = {command.FlagParam}");

            // Assert
            Assert.True(command.FlagParam);
        }
    }
}
