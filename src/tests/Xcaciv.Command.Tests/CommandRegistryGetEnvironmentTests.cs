using System;
using Xunit;
using Xcaciv.Command;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Tests.TestImplementations;

namespace Xcaciv.Command.Tests
{
    public class CommandRegistryGetEnvironmentTests
    {
        [Fact]
        public void GetEnvironment_WithNullFactory_ThrowsArgumentNullException()
        {
            var registry = new CommandRegistry();
            Assert.Throws<ArgumentNullException>(() => registry.GetEnvironment(null!));
        }

        [Fact]
        public void GetEnvironment_WithEmptyRegistry_ReturnsEmptyEnvironment()
        {
            var registry = new CommandRegistry();
            var factory = new CommandFactory();

            var environment = registry.GetEnvironment(factory);

            Assert.NotNull(environment);
            Assert.Empty(environment.GetEnvironment());
            Assert.Empty(environment.GetCommandEnvironmentNames());
        }

        [Fact]
        public void GetEnvironment_WithSingleCommand_CollectsEnvironment()
        {
            var registry = new CommandRegistry();
            var factory = new CommandFactory();
            
            registry.AddCommand("test", typeof(FetchTestCommand));

            var environment = registry.GetEnvironment(factory);

            Assert.NotNull(environment);
            var commandNames = environment.GetCommandEnvironmentNames();
            Assert.Single(commandNames);
            Assert.Contains("FETCH", commandNames);

            var fetchEnv = environment.GetEnvironment("FETCH");
            Assert.Equal(3, fetchEnv.Count);
            Assert.Equal("30", fetchEnv["FETCH_TIMEOUT"]);
        }

        [Fact]
        public void GetEnvironment_WithMultipleCommands_CollectsAll()
        {
            var registry = new CommandRegistry();
            var factory = new CommandFactory();

            registry.AddCommand("test", typeof(FetchTestCommand));
            registry.AddCommand("test", typeof(ProcessTestCommand));
            registry.AddCommand("test", typeof(LogTestCommand));

            var environment = registry.GetEnvironment(factory);

            var commandNames = environment.GetCommandEnvironmentNames();
            Assert.Equal(3, commandNames.Count);
            Assert.Contains("FETCH", commandNames);
            Assert.Contains("PROCESS", commandNames);
            Assert.Contains("LOG", commandNames);
        }

        [Fact]
        public void GetEnvironment_CommandWithNoDefaults_NotIncluded()
        {
            var registry = new CommandRegistry();
            var factory = new CommandFactory();

            registry.AddCommand("test", typeof(WithEnvTestCommand));
            registry.AddCommand("test", typeof(WithoutEnvTestCommand));

            var environment = registry.GetEnvironment(factory);

            var commandNames = environment.GetCommandEnvironmentNames();
            Assert.Single(commandNames);
            Assert.Contains("WITH_ENV", commandNames);
            Assert.DoesNotContain("WITHOUT_ENV", commandNames);
        }

        [Fact]
        public void GetEnvironment_WithSubCommands_CollectsAll()
        {
            var registry = new CommandRegistry();
            var factory = new CommandFactory();

            registry.AddCommand("test", typeof(GitRootTestCommand));
            registry.AddCommand("test", typeof(GitCloneTestCommand));
            registry.AddCommand("test", typeof(GitPushTestCommand));

            var environment = registry.GetEnvironment(factory);

            var commandNames = environment.GetCommandEnvironmentNames();
            
            Assert.Contains("GIT", commandNames);
            Assert.Contains("CLONE", commandNames);
            Assert.Contains("PUSH", commandNames);

            var gitEnv = environment.GetEnvironment("GIT");
            Assert.Equal(2, gitEnv.Count);
            Assert.Equal("John Doe", gitEnv["GIT_USER_NAME"]);
        }

        [Fact]
        public void GetEnvironment_CalledMultipleTimes_ReturnsConsistentResults()
        {
            var registry = new CommandRegistry();
            var factory = new CommandFactory();

            registry.AddCommand("test", typeof(TestConsistentCommand));

            var env1 = registry.GetEnvironment(factory);
            var env2 = registry.GetEnvironment(factory);

            var testEnv1 = env1.GetEnvironment("TESTCONSISTENT");
            var testEnv2 = env2.GetEnvironment("TESTCONSISTENT");

            Assert.Equal(testEnv1.Count, testEnv2.Count);
            Assert.Equal(testEnv1["TESTCONSISTENT_VALUE1"], testEnv2["TESTCONSISTENT_VALUE1"]);
        }
    }
}
