using Xunit;
using Xunit.Abstractions;
using Moq;
using System.Linq;
using Xcaciv.Command.Tests.Commands;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Tests
{
    public class FieldInjectionDebugTests
    {
        private readonly ITestOutputHelper _output;

        public FieldInjectionDebugTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Debug_ParameterProcessing()
        {
            // Arrange
            var command = new FieldInjectionTestCommand();
            var ioContext = new Mock<IIoContext>();
            // Flags don't take values - they're boolean presence indicators
            // Named params need their value immediately after
            // Suffix catches everything at the end
            ioContext.Setup(x => x.Parameters).Returns(new[] { "value1", "value2", "--NamedParam", "namedValue", "--FlagParam", "suffixValue" });
            ioContext.Setup(x => x.HasPipedInput).Returns(false);

            // Act
            var processedParams = command.ProcessParameters(ioContext.Object);

            // Debug output
            _output.WriteLine("Processed Parameters:");
            foreach (var kvp in processedParams)
            {
                var valueStr = kvp.Value.RawValue?.ToString() ?? "null";
                _output.WriteLine($"  {kvp.Key} = '{valueStr}' (Type: {kvp.Value.DataType.Name}, IsValid: {kvp.Value.IsValid})");
            }

            _output.WriteLine("\nField Values:");
            _output.WriteLine($"  FirstParam = '{command.FirstParam ?? "null"}'");
            _output.WriteLine($"  SecondParam = '{command.SecondParam ?? "null"}'");
            _output.WriteLine($"  NamedParam = '{command.NamedParam ?? "null"}'");
            _output.WriteLine($"  FlagParam = {command.FlagParam}");
            _output.WriteLine($"  SuffixParam = '{command.SuffixParam ?? "null"}'");

            // Check what's actually in the dictionary
            Assert.True(processedParams.ContainsKey("FirstParam"));
            Assert.True(processedParams.ContainsKey("NamedParam"));
        }
    }
}
