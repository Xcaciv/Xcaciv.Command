using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.FileLoader;
using Xunit.Abstractions;

namespace Xcaciv.Command.Tests.Commands
{
    public class RegifCommandTests
    {
        private ITestOutputHelper _testOutput;
        private string commandPackageDir = @"..\..\..\..\zTestCommandPackage\bin\{1}\";
        public RegifCommandTests(ITestOutputHelper output)
        {
            _testOutput = output;
#if DEBUG
            _testOutput.WriteLine("Tests in Debug mode");
            commandPackageDir = commandPackageDir.Replace("{1}", "Debug");
#else
            _testOutput.WriteLine("Tests in Release mode??");
            commandPackageDir = commandPackageDir.Replace("{1}", "Release");
#endif
        }

        [Fact()]
        public async Task HandleExecutionTestAsync()
        {
            var commands = new CommandController(new Crawler(), @"..\..\..\..\..\");
            commands.AddPackageDirectory(commandPackageDir);
            commands.RegisterBuiltInCommands();
            commands.LoadCommands(string.Empty);

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input
            await commands.Run("echo what is up | regif is", textio, env);

            // verify the output of the first run
            // by looking at the output of the second output line
            Assert.Equal("is", textio.ToString().Trim());
        }

        [Fact()]
        public async Task HandleExecutionWithQuotesTestAsync()
        {
            var commands = new CommandController(new Crawler(), @"..\..\..\..\..\");
            commands.AddPackageDirectory(commandPackageDir);
            commands.RegisterBuiltInCommands();
            commands.LoadCommands(string.Empty);

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input
            await commands.Run("echo 'what is | up' | regif is", textio, env);

            // verify the output of the first run
            // by looking at the output of the second output line
            Assert.Equal("is", textio.ToString().Trim());
        }

        [Fact()]
        public async Task HandleExecutionWithSetTestAsync()
        {
            var commands = new CommandController(new Crawler(), @"..\..\..\..\..\");
            commands.AddPackageDirectory(commandPackageDir);
            commands.RegisterBuiltInCommands();
            commands.LoadCommands(string.Empty);

            var env = new EnvironmentContext();
            var textio = new TestImpementations.TestTextIo();
            // simulate user input - pipeline sets variable but produces no output
            await commands.Run("echo 'what is up' | regif is | set found", textio, env);

            // Assert - verify the variable was set correctly
            Assert.Equal("is", env.GetValue("found"));
        }
    }
}
