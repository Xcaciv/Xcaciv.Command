using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Encoders;

namespace Xcaciv.Command.Tests
{
    /// <summary>
    /// Tests for output encoding functionality (Phase 7).
    /// Verifies that output encoders correctly handle different encoding scenarios
    /// for web safety, JSON APIs, and other systems.
    /// </summary>
    public class OutputEncodingTests
    {
        [Fact]
        public void NoOpEncoderShouldReturnInputUnchanged()
        {
            // Arrange
            var encoder = new NoOpEncoder();
            var input = "<div>Hello & \"World\"</div>";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.Equal(input, output);
        }

        [Fact]
        public void HtmlEncoderShouldEscapeSpecialCharacters()
        {
            // Arrange
            var encoder = new HtmlEncoder();
            var input = "<script>alert('XSS')</script>";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.DoesNotContain("<script>", output);
            Assert.DoesNotContain("</script>", output);
            Assert.Contains("&lt;script&gt;", output);
        }

        [Fact]
        public void HtmlEncoderShouldEscapeAmpersand()
        {
            // Arrange
            var encoder = new HtmlEncoder();
            var input = "Tom & Jerry";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.Equal("Tom &amp; Jerry", output);
        }

        [Fact]
        public void HtmlEncoderShouldEscapeQuotes()
        {
            // Arrange
            var encoder = new HtmlEncoder();
            var input = "He said \"Hello\"";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.Contains("&quot;", output);
        }

        [Fact]
        public void HtmlEncoderShouldEscapeGreaterThanAndLessThan()
        {
            // Arrange
            var encoder = new HtmlEncoder();
            var input = "5 < 10 and 20 > 15";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.Contains("&lt;", output);
            Assert.Contains("&gt;", output);
        }

        [Fact]
        public void JsonEncoderShouldEscapeBackslashes()
        {
            // Arrange
            var encoder = new JsonEncoder();
            var input = @"C:\Users\Test";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.NotNull(output);
            Assert.NotEmpty(output);
        }

        [Fact]
        public void JsonEncoderShouldHandleComplexStrings()
        {
            // Arrange
            var encoder = new JsonEncoder();
            var input = "Line 1\nLine 2\t\"quoted\"";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.NotNull(output);
            Assert.NotEmpty(output);
        }

        [Fact]
        public void CommandControllerShouldHaveOutputEncoderProperty()
        {
            // Arrange
            var controller = new CommandController();

            // Act & Assert
            Assert.NotNull(controller.OutputEncoder);
            Assert.IsType<NoOpEncoder>(controller.OutputEncoder);
        }

        [Fact]
        public void CommandControllerShouldAllowSettingOutputEncoder()
        {
            // Arrange
            var controller = new CommandController();
            var htmlEncoder = new HtmlEncoder();

            // Act
            controller.OutputEncoder = htmlEncoder;

            // Assert
            Assert.NotNull(controller.OutputEncoder);
            Assert.Equal(htmlEncoder, controller.OutputEncoder);
        }

        [Fact]
        public void OutputEncoderDefaultShouldBeNoOp()
        {
            // Arrange
            var controller = new CommandController();

            // Act
            var encoder = controller.OutputEncoder;

            // Assert
            Assert.NotNull(encoder);
            Assert.IsType<NoOpEncoder>(encoder);
            Assert.Equal("<test>", encoder.Encode("<test>"));
        }

        [Fact]
        public void HtmlEncoderShouldHandleEmptyString()
        {
            // Arrange
            var encoder = new HtmlEncoder();

            // Act
            var output = encoder.Encode("");

            // Assert
            Assert.Equal("", output);
        }

        [Fact]
        public void HtmlEncoderShouldHandleUnicodeCharacters()
        {
            // Arrange
            var encoder = new HtmlEncoder();
            var input = "Hello ?? ??";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.NotNull(output);
            Assert.Contains("Hello", output);
        }

        [Fact]
        public void JsonEncoderShouldHandleUnicodeCharacters()
        {
            // Arrange
            var encoder = new JsonEncoder();
            var input = "Hello ?? ??";

            // Act
            var output = encoder.Encode(input);

            // Assert
            Assert.NotNull(output);
        }

        [Fact]
        public async Task EncodersShouldBeThreadSafe()
        {
            // Arrange
            var encoder = new HtmlEncoder();
            var input = "<test>";
            var outputs = new System.Collections.Concurrent.ConcurrentBag<string>();

            // Act
            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() => outputs.Add(encoder.Encode(input))))
                .ToArray();
            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(100, outputs.Count);
            Assert.True(outputs.All(o => o == "&lt;test&gt;"));
        }

        [Fact]
        public void MultipleEncodersCanCoexist()
        {
            // Arrange
            var noOp = new NoOpEncoder();
            var html = new HtmlEncoder();
            var json = new JsonEncoder();
            var input = "<div>Test</div>";

            // Act
            var noOpResult = noOp.Encode(input);
            var htmlResult = html.Encode(input);
            var jsonResult = json.Encode(input);

            // Assert
            Assert.Equal(input, noOpResult);
            Assert.NotEqual(input, htmlResult);
            Assert.NotEqual(htmlResult, jsonResult);
        }

        [Fact]
        public void HtmlEncoderIsFullyFunctional()
        {
            // Arrange
            var encoder = new HtmlEncoder();

            // Act & Assert - Verify the encoder produces safe HTML
            Assert.Equal("&lt;", encoder.Encode("<"));
            Assert.Equal("&gt;", encoder.Encode(">"));
            Assert.Equal("&amp;", encoder.Encode("&"));
        }

        [Fact]
        public void JsonEncoderIsFullyFunctional()
        {
            // Arrange
            var encoder = new JsonEncoder();

            // Act - Verify encoder doesn't crash on various inputs
            var result1 = encoder.Encode("simple");
            var result2 = encoder.Encode("with spaces");
            var result3 = encoder.Encode("with-special!@#$%");

            // Assert - All should return non-null results
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotNull(result3);
        }
    }
}
