using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Commands;
using Xcaciv.Command.Core;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xunit;

namespace Xcaciv.Command.Tests.Commands
{
    public class TestSubCommandTests
    {
        [Fact()]
        public async Task ParameterTest()
        {
            var commands = new CommandController(new Crawler(), AppContext.BaseDirectory);
            commands.RegisterBuiltInCommands();
            commands.AddCommand("ignored", typeof(TestSubCommand));

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input
            await commands.Run("package search nuget", textio, env);

            // Check all available output (includes children)
            var allOutput = textio.ToString();

            // The command should produce some output
            Assert.NotEmpty(allOutput);

            // Verify no errors in the output
            Assert.DoesNotContain("ERROR:", allOutput);

            // Verify it's showing parameters (basic smoke test)
            Assert.Contains("Parameters", allOutput);
        }
        [Fact()]
        public async Task HandleShortCommandExecutionTest()
        {
            var commands = new CommandController(new Crawler(), AppContext.BaseDirectory);
            commands.RegisterBuiltInCommands();
            commands.AddCommand("ignored", typeof(TestSubCommand));

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input
            await commands.Run("package search nuget", textio, env);

            // Check all available output (includes children)
            var allOutput = textio.ToString();

            // The command should produce some output
            Assert.NotEmpty(allOutput);

            // Verify no errors in the output
            Assert.DoesNotContain("ERROR:", allOutput);

            // Verify it's showing parameters (basic smoke test)
            Assert.Contains("Parameters", allOutput);
        }
        [Fact()]
        public async Task HandleLongCommandExecutionTest()
        {
            var commands = new CommandController(new Crawler(), AppContext.BaseDirectory);
            commands.RegisterBuiltInCommands();
            commands.AddCommand("ignored", typeof(TestSubCommand));

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input
            await commands.Run("package search \"some phrase\" -tAke 99 -prerelease -verbosity normal", textio, env);

            // Check all available output (includes children)
            var allOutput = textio.ToString();
            
            // The command should produce some output
            Assert.NotEmpty(allOutput);
            
            // Verify no errors in the output
            Assert.DoesNotContain("ERROR:", allOutput);
            
            // Verify it's showing parameters (basic smoke test)
            Assert.Contains("Parameters", allOutput);
        }

        [Fact()]
        public async Task HandleExecutionPipeTest()
        {
            var commands = new CommandController(new Crawler(), AppContext.BaseDirectory);
            commands.RegisterBuiltInCommands();
            commands.AddCommand("ignored", typeof(TestSubCommand));

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input
            await commands.Run("say some phrase | package search -tAke 99 -prerelease -verbosity normal", textio, env);

            // Check all available output
            var allOutput = textio.ToString();
            
            // The pipeline should produce some output
            Assert.NotEmpty(allOutput);
            
            // Verify no errors in the output
            Assert.DoesNotContain("ERROR:", allOutput);
            
            // Verify we got expected content - either in the aggregated output or trace
            Assert.True(allOutput.Contains("Parameters") || allOutput.Contains("Piped Chunk"), 
                $"Expected output to contain 'Parameters' or 'Piped Chunk', got: {allOutput}");
        }

        //[Fact()]
        //public void ProcessEnvValuesTest()
        //{
        //    var env = new EnvironmentContext();
        //    var textio = new TestImpementations.TestTextIo();
        //    env.SetValue("direction", "up");

        //    var actual = SayCommand.ProcessEnvValues("what is %direction%!", env);

        //    Assert.Equal("what is up!", actual);
        //}

        //[Fact()]
        //public async Task HandleExecutionWithEnvTest()
        //{
        //    var commands = new CommandController(new Crawler(), AppContext.BaseDirectory);
        //    commands.RegisterBuiltInCommands();

        //    var env = new EnvironmentContext();
        //    var textio = new TestImpementations.TestTextIo();
        //    env.SetValue("direction", "up");
        //    // simulate user input
        //    await commands.Run(@"say ""what is %direction%!""", textio, env);

        //    // verify the output of the first run
        //    // by looking at the output of the second output line
        //    Assert.Equal("what is up!", textio.Children.First().Output.First());
        //}

        //[Fact()]
        //public void BaseAttributeTest()
        //{
        //    var attributes = Attribute.GetCustomAttribute(typeof(SayCommand), typeof(CommandRegisterAttribute)) as CommandRegisterAttribute;

        //    Assert.NotNull(attributes);
        //    Assert.Equal("Like echo but more valley.", attributes.Description);
        //}

        //[Fact()]
        //public void ParameterAttributeTest()
        //{

        //    var attributes = Attribute.GetCustomAttributes(typeof(SayCommand), typeof(CommandParameterSuffixAttribute)) as CommandParameterSuffixAttribute[];

        //    Assert.NotNull(attributes);
        //    Assert.NotEmpty(attributes);
        //    Assert.Equal("text", attributes.First().Name);
        //}

        //[Fact()]
        //public void MultipleParameterAttributeTest()
        //{

        //    var attributes = Attribute.GetCustomAttributes(typeof(SayCommand), typeof(CommandHelpRemarksAttribute)) as CommandHelpRemarksAttribute[];

        //    Assert.NotNull(attributes);
        //    Assert.NotEmpty(attributes);
        //    Assert.Equal(2, attributes.Length);
        //}

        //// test the one line help string
        //[Fact()]
        //public void OneLineHelpTest()
        //{
        //    var textio = new TestImpementations.TestTextIo();
        //    var command = new SayCommand();

        //    var result = command.OneLineHelp(textio.Parameters);

        //    Assert.Equal("SAY          Like echo but more valley.", result);
        //}
    }
}
