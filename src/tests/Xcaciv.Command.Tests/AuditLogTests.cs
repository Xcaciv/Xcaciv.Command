using Xunit;
using Xcaciv.Command;
using Xcaciv.Command.Tests.TestImpementations;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xcaciv.Command.Tests
{
    public partial class AuditLogTests
    {
        private ITestOutputHelper _testOutput;

        public AuditLogTests(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        /// <summary>
        /// Test: NoOpAuditLogger doesn't throw when logging
        /// </summary>
        [Fact]
        public void NoOpAuditLogger_LogsWithoutError()
        {
            // Arrange
            var logger = new NoOpAuditLogger();
            var now = DateTime.UtcNow;

            // Act & Assert - Should not throw
            logger.LogCommandExecution("testcmd", new[] { "arg1", "arg2" }, now, TimeSpan.FromSeconds(1), true);
            logger.LogEnvironmentChange("VAR", "old", "new", "test", now);
        }

        /// <summary>
        /// Test: Custom audit logger captures command execution
        /// </summary>
        [Fact]
        public async Task CustomAuditLogger_CapturesCommandExecutionAsync()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.AuditLogger = logger;
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "hello" });

            // Act
            await controller.Run("say hello", ioContext, env);

            // Assert - Check new AuditEvents structure
            Assert.NotEmpty(logger.AuditEvents);
            var auditEvent = logger.AuditEvents[0];
            Assert.Equal("SAY", auditEvent.CommandName);
            Assert.Equal("hello", auditEvent.Parameters[0]);
            Assert.True(auditEvent.Success);
            Assert.NotNull(auditEvent.CorrelationId); // New in v2.0
            Assert.NotNull(auditEvent.PackageOrigin); // New in v2.0
        }

        /// <summary>
        /// Test: Audit logger captures execution duration
        /// </summary>
        [Fact]
        public async Task AuditLogger_CapturesDurationAsync()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.AuditLogger = logger;
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "test" });

            // Act
            await controller.Run("say test", ioContext, env);

            // Assert
            Assert.NotEmpty(logger.AuditEvents);
            var auditEvent = logger.AuditEvents[0];
            Assert.True(auditEvent.Duration >= TimeSpan.Zero);
            Assert.NotEqual(default(DateTime), auditEvent.ExecutedAt);
        }

        /// <summary>
        /// Test: Audit logger records command execution with timestamp
        /// </summary>
        [Fact]
        public async Task AuditLogger_RecordsTimestampAsync()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.AuditLogger = logger;
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "test" });
            var beforeTime = DateTime.UtcNow;

            // Act
            await controller.Run("say test", ioContext, env);

            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.NotEmpty(logger.AuditEvents);
            var auditEvent = logger.AuditEvents[0];
            Assert.True(auditEvent.ExecutedAt >= beforeTime);
            Assert.True(auditEvent.ExecutedAt <= afterTime);
        }

        /// <summary>
        /// Test: Environment changes are logged
        /// </summary>
        [Fact]
        public void EnvironmentContext_LogsChanges()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var env = new EnvironmentContext();
            env.SetAuditLogger(logger);

            // Act
            env.SetValue("TESTVAR", "value1");
            env.SetValue("TESTVAR", "value2");

            // Assert
            Assert.NotEmpty(logger.EnvironmentLogs);
            Assert.Equal(2, logger.EnvironmentLogs.Count);
            
            var firstLog = logger.EnvironmentLogs[0];
            Assert.Equal("TESTVAR", firstLog.VariableName);
            Assert.Null(firstLog.OldValue);
            Assert.Equal("value1", firstLog.NewValue);

            var secondLog = logger.EnvironmentLogs[1];
            Assert.Equal("TESTVAR", secondLog.VariableName);
            Assert.Equal("value1", secondLog.OldValue);
            Assert.Equal("value2", secondLog.NewValue);
        }

        /// <summary>
        /// Test: Child environment inherits parent's audit logger
        /// </summary>
        [Fact]
        public async Task ChildEnvironment_InheritsAuditLoggerAsync()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var parentEnv = new EnvironmentContext();
            parentEnv.SetAuditLogger(logger);

            // Act
            var childEnv = (EnvironmentContext)await parentEnv.GetChild();
            childEnv.SetValue("CHILDVAR", "childvalue");

            // Assert
            Assert.NotEmpty(logger.EnvironmentLogs);
            var log = logger.EnvironmentLogs[0];
            Assert.Equal("CHILDVAR", log.VariableName);
            Assert.Equal("childvalue", log.NewValue);
        }

        /// <summary>
        /// Test: Audit logger receives sanitized parameters
        /// </summary>
        [Fact]
        public async Task AuditLogger_ReceivesSanitizedParametersAsync()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.AuditLogger = logger;
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "test1", "test2" });

            // Act
            await controller.Run("say test1 test2", ioContext, env);

            // Assert
            Assert.NotEmpty(logger.AuditEvents);
            var auditEvent = logger.AuditEvents[0];
            Assert.NotNull(auditEvent.Parameters);
            Assert.Equal("SAY", auditEvent.CommandName);
        }

        /// <summary>
        /// Test: Multiple command executions are logged separately
        /// </summary>
        [Fact]
        public async Task AuditLogger_LogsMultipleExecutionsAsync()
        {
            // Arrange
            var logger = new TestAuditLogger();
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.AuditLogger = logger;
            var env = new EnvironmentContext();

            // Act
            var ioContext1 = new TestTextIo(new[] { "first" });
            await controller.Run("say first", ioContext1, env);

            var ioContext2 = new TestTextIo(new[] { "second" });
            await controller.Run("say second", ioContext2, env);

            var ioContext3 = new TestTextIo(new[] { "third" });
            await controller.Run("say third", ioContext3, env);

            // Assert
            Assert.Equal(3, logger.AuditEvents.Count);
            Assert.Equal("SAY", logger.AuditEvents[0].CommandName);
            Assert.Equal("SAY", logger.AuditEvents[1].CommandName);
            Assert.Equal("SAY", logger.AuditEvents[2].CommandName);
        }
    }
}
