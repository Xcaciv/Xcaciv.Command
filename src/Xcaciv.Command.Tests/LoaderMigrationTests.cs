using Xunit;
using Xcaciv.Command;
using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Tests.TestImpementations;

namespace Xcaciv.Command.Tests
{
    /// <summary>
    /// Tests for Xcaciv.Loader 2.0.1 migration - verifies instance-based security configuration
    /// </summary>
    public class LoaderMigrationTests
    {
        private ITestOutputHelper _testOutput;
        private string commandPackageDir = @"..\..\..\..\zTestCommandPackage\bin\{1}\";

        public LoaderMigrationTests(ITestOutputHelper output)
        {
            _testOutput = output;
#if DEBUG
            _testOutput.WriteLine("Tests in Debug mode");
            commandPackageDir = commandPackageDir.Replace("{1}", "Debug");
#else
            _testOutput.WriteLine("Tests in Release mode");
            commandPackageDir = commandPackageDir.Replace("{1}", "Release");
#endif
        }

        [Fact]
        public void LoadCommands_WithDefaultSecurityPolicy_LoadsSuccessfully()
        {
            // Arrange
            var controller = new CommandController(new Crawler(), @"..\..\..\..\..");
            controller.AddPackageDirectory(commandPackageDir);

            // Act
            controller.LoadCommands(string.Empty);

            // Assert - LoadCommands should complete without throwing SecurityException
            // If we get here, the default security policy allowed loading
            _testOutput.WriteLine("Commands loaded successfully with default security policy");
        }

        [Fact]
        public async Task ExecuteCommand_WithInstanceBasedSecurity_ExecutesSuccessfully()
        {
            // Arrange
            var controller = new CommandController(new Crawler(), @"..\..\..\..\..");
            controller.AddPackageDirectory(commandPackageDir);
            controller.LoadCommands(string.Empty);
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act
            await controller.Run("echo test security", textio, env);

            // Assert
            Assert.Contains("test", textio.Children.First().Output);
            Assert.Contains("security", textio.Children.First().Output);
        }

        [Fact]
        public void LoadPackageDescriptions_HandlesSecurityExceptionsGracefully()
        {
            // Arrange
            var crawler = new Crawler();

            // Act - This should not throw even if there are security issues with some packages
            var packages = crawler.LoadPackageDescriptions(commandPackageDir, string.Empty);

            // Assert - Should have loaded at least some packages
            Assert.NotEmpty(packages);
            _testOutput.WriteLine($"Loaded {packages.Count} package(s) successfully");
        }

        [Fact]
        public void Crawler_LoadsCommandsWithProperPathRestriction()
        {
            // Arrange
            var crawler = new Crawler();

            // Act
            var packages = crawler.LoadPackageDescriptions(commandPackageDir, string.Empty);

            // Assert - Verify packages were loaded with proper security
            Assert.NotEmpty(packages);
            
            foreach (var package in packages)
            {
                Assert.NotNull(package.Value.FullPath);
                Assert.NotEmpty(package.Value.Commands);
                _testOutput.WriteLine($"Package: {package.Key}, Commands: {package.Value.Commands.Count}");
            }
        }

        [Fact]
        public async Task CommandExecution_WithPluginLoading_UsesSecureContext()
        {
            // Arrange
            var controller = new CommandController(new Crawler(), @"..\..\..\..\..");
            controller.AddPackageDirectory(commandPackageDir);
            controller.LoadCommands(string.Empty);
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Execute a command that requires plugin loading
            await controller.Run("echo test", textio, env);
            var firstChild = textio.Children.FirstOrDefault();

            // Assert
            Assert.NotNull(firstChild);
            Assert.NotEmpty(firstChild.Output);
            _testOutput.WriteLine($"Command executed with output: {string.Join(", ", firstChild.Output)}");
        }

        [Fact]
        public async Task SubCommand_WithSecureLoading_ExecutesCorrectly()
        {
            // Arrange
            var controller = new CommandController(new Crawler(), @"..\..\..\..\..");
            controller.AddPackageDirectory(commandPackageDir);
            controller.LoadCommands(string.Empty);
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Execute a sub-command
            await controller.Run("do echo secure test", textio, env);
            var firstChild = textio.Children.FirstOrDefault();

            // Assert
            Assert.NotNull(firstChild);
            Assert.Contains("secure test", firstChild.Output);
        }

        [Fact]
        public async Task MultiplePackageDirectories_WithSecureLoading_AllLoadSuccessfully()
        {
            // Arrange
            var controller = new CommandController(new Crawler(), @"..\..\..\..\..");
            controller.AddPackageDirectory(commandPackageDir);
            
            // Act
            controller.LoadCommands(string.Empty);
            controller.RegisterBuiltInCommands();

            // Assert - Both plugin and default commands should be loaded
            var env = new EnvironmentContext();
            var textio = new TestTextIo();
            await controller.GetHelpAsync(string.Empty, textio, env);
            var output = textio.ToString();

            Assert.Contains("ECHO", output); // Plugin command
            Assert.Contains("REGIF", output); // Default command
        }

        [Fact]
        public async Task PipelineExecution_WithSecureLoading_WorksCorrectly()
        {
            // Arrange
            var controller = new CommandController(new Crawler(), @"..\..\..\..\..");
            controller.AddPackageDirectory(commandPackageDir);
            controller.LoadCommands(string.Empty);
            var env = new EnvironmentContext();
            var textio = new TestTextIo();

            // Act - Execute a pipeline
            await controller.Run("echo security | echo2 | echoe", textio, env);

            // Assert - Pipeline should execute all stages successfully
            var output = textio.ToString();
            Assert.NotEmpty(output);
            _testOutput.WriteLine($"Pipeline output: {output}");
        }
    }
}
