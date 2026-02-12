using Xcaciv.Command.Interface;
using System;
using System.Collections.Generic;

namespace Xcaciv.Command.Tests
{
    public partial class AuditLogTests
    {
        /// <summary>
        /// Test audit logger that captures all logs for testing
        /// </summary>
        private class TestAuditLogger : IAuditLogger
        {
            public List<CommandExecutionLog> ExecutionLogs { get; } = new();
            public List<EnvironmentChangeLog> EnvironmentLogs { get; } = new();
            public List<AuditEvent> AuditEvents { get; } = new();

            public IAuditMaskingConfiguration? MaskingConfiguration { get; set; }

            public void LogCommandExecution(
                string commandName,
                string[] parameters,
                DateTime executedAt,
                TimeSpan duration,
                bool success,
                string? errorMessage = null)
            {
                ExecutionLogs.Add(new CommandExecutionLog
                {
                    CommandName = commandName,
                    Parameters = parameters,
                    ExecutedAt = executedAt,
                    Duration = duration,
                    Success = success,
                    ErrorMessage = errorMessage
                });
            }

            public void LogEnvironmentChange(
                string variableName,
                string? oldValue,
                string? newValue,
                string changedBy,
                DateTime changedAt)
            {
                EnvironmentLogs.Add(new EnvironmentChangeLog
                {
                    VariableName = variableName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangedBy = changedBy,
                    ChangedAt = changedAt
                });
            }

            public void LogAuditEvent(AuditEvent auditEvent)
            {
                AuditEvents.Add(auditEvent);
            }

            public class CommandExecutionLog
            {
                public string CommandName { get; set; } = string.Empty;
                public string[] Parameters { get; set; } = Array.Empty<string>();
                public DateTime ExecutedAt { get; set; }
                public TimeSpan Duration { get; set; }
                public bool Success { get; set; }
                public string? ErrorMessage { get; set; }
            }

            public class EnvironmentChangeLog
            {
                public string VariableName { get; set; } = string.Empty;
                public string? OldValue { get; set; }
                public string? NewValue { get; set; }
                public string ChangedBy { get; set; } = string.Empty;
                public DateTime ChangedAt { get; set; }
            }
        }
    }
}
