using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests.TestImplementations
{
    /// <summary>
    /// Test command that simulates a FETCH command with realistic environment variables.
    /// </summary>
    [CommandRegister("FETCH", "Fetch command with connection settings")]
    public class FetchTestCommand : ICommandDelegate
    {
        public string Command => "FETCH";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "TIMEOUT", "30" },
                { "MAX_RETRIES", "3" },
                { "BASE_URL", "https://api.example.com" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("FETCH command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that simulates a PROCESS command with configuration settings.
    /// </summary>
    [CommandRegister("PROCESS", "Process command with batch settings")]
    public class ProcessTestCommand : ICommandDelegate
    {
        public string Command => "PROCESS";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BATCH_SIZE", "100" },
                { "PARALLEL", "true" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("PROCESS command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that simulates a LOG command with logging settings.
    /// </summary>
    [CommandRegister("LOG", "Log command with configuration")]
    public class LogTestCommand : ICommandDelegate
    {
        public string Command => "LOG";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "LEVEL", "INFO" },
                { "FILE_PATH", "/var/log/app.log" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("LOG command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that has a single environment variable.
    /// </summary>
    [CommandRegister("WITH_ENV", "Command with one env var")]
    public class WithEnvTestCommand : ICommandDelegate
    {
        public string Command => "WITH_ENV";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "VALUE", "test" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("WITH_ENV command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that has no default environment variables.
    /// </summary>
    [CommandRegister("WITHOUT_ENV", "Command with no default environment")]
    public class WithoutEnvTestCommand : ICommandDelegate
    {
        public string Command => "WITHOUT_ENV";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("WITHOUT_ENV command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Small test command with one environment variable.
    /// </summary>
    [CommandRegister("SMALL", "Small command")]
    public class SmallTestCommand : ICommandDelegate
    {
        public string Command => "SMALL";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "KEY1", "value1" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("SMALL command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Medium test command with three environment variables.
    /// </summary>
    [CommandRegister("MEDIUM", "Medium command")]
    public class MediumTestCommand : ICommandDelegate
    {
        public string Command => "MEDIUM";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "KEY1", "value1" },
                { "KEY2", "value2" },
                { "KEY3", "value3" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("MEDIUM command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Large test command with many environment variables.
    /// </summary>
    [CommandRegister("LARGE", "Large command")]
    public class LargeTestCommand : ICommandDelegate
    {
        public string Command => "LARGE";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "CONFIG_PATH", "/etc/app/config.yaml" },
                { "DATA_DIR", "/var/data" },
                { "CACHE_SIZE", "1024" },
                { "ENABLE_METRICS", "true" },
                { "LOG_LEVEL", "DEBUG" },
                { "MAX_CONNECTIONS", "50" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("LARGE command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Root GIT command with user configuration.
    /// </summary>
    [CommandRegister("GIT", "Git root command")]
    public class GitRootTestCommand : ICommandDelegate
    {
        public string Command => "GIT";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "USER_NAME", "John Doe" },
                { "USER_EMAIL", "john@example.com" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("GIT command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// GIT CLONE subcommand.
    /// </summary>
    [CommandRegister("CLONE", "Git clone subcommand")]
    [CommandRoot("GIT", "Git root")]
    public class GitCloneTestCommand : ICommandDelegate
    {
        public string Command => "CLONE";
        public string RootCommand => "GIT";

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "DEPTH", "1" },
                { "RECURSIVE", "false" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("GIT CLONE command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// GIT PUSH subcommand.
    /// </summary>
    [CommandRegister("PUSH", "Git push subcommand")]
    [CommandRoot("GIT", "Git root")]
    public class GitPushTestCommand : ICommandDelegate
    {
        public string Command => "PUSH";
        public string RootCommand => "GIT";

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "FORCE", "false" },
                { "TAGS", "true" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("GIT PUSH command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command with both prefixed and unprefixed keys.
    /// </summary>
    [CommandRegister("TESTPREFIX", "Test command for prefix testing")]
    public class TestPrefixCommand : ICommandDelegate
    {
        public string Command => "TESTPREFIX";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "VALUE", "unprefixed" },
                { "TESTPREFIX_VALUE2", "already_prefixed" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("TESTPREFIX command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// API command with mixed-case keys.
    /// </summary>
    [CommandRegister("APITEST", "API test command")]
    public class ApiCaseTestCommand : ICommandDelegate
    {
        public string Command => "APITEST";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "endpoint", "https://api.example.com" },
                { "TIMEOUT", "30" },
                { "MaxRetries", "5" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("APITEST command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that simulates a database connection with realistic environment variables.
    /// </summary>
    [CommandRegister("DB", "Database command with connection settings")]
    public class DatabaseTestCommand : ICommandDelegate
    {
        public string Command => "DB";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOST", "localhost" },
                { "PORT", "5432" },
                { "DATABASE", "testdb" },
                { "USERNAME", "postgres" },
                { "PASSWORD", "" },
                { "MAX_POOL_SIZE", "20" },
                { "MIN_POOL_SIZE", "5" },
                { "CONNECTION_TIMEOUT", "30" },
                { "COMMAND_TIMEOUT", "30" },
                { "SSL_MODE", "prefer" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("Database command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that simulates a database connection with special characters.
    /// </summary>
    [CommandRegister("DBSPECIAL", "Database with special characters")]
    public class DbSpecialTestCommand : ICommandDelegate
    {
        public string Command => "DBSPECIAL";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "CONNECTION_STRING", "Server=localhost;Database=test;User=admin;Password=P@ssw0rd!" },
                { "BACKUP_PATH", @"C:\backups\db\2024-01-15" },
                { "QUERY", "SELECT * FROM users WHERE email LIKE '%@example.com'" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("DBSPECIAL command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Database command for real-world scenario test.
    /// </summary>
    [CommandRegister("DATABASE", "Database command")]
    public class DatabaseRealWorldCommand : ICommandDelegate
    {
        public string Command => "DATABASE";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOST", "localhost" },
                { "PORT", "5432" },
                { "NAME", "myapp_db" },
                { "USER", "dbuser" },
                { "PASSWORD", "" }, // Empty password for local dev
                { "MAX_CONNECTIONS", "20" },
                { "TIMEOUT", "30" },
                { "SSL_MODE", "prefer" },
                { "POOL_SIZE", "10" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("DATABASE command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// API command for microservice scenario.
    /// </summary>
    [CommandRegister("APISERVICE", "API service command")]
    public class ApiServiceCommand : ICommandDelegate
    {
        public string Command => "APISERVICE";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "PORT", "8080" },
                { "HOST", "0.0.0.0" },
                { "BASE_PATH", "/api/v1" },
                { "CORS_ENABLED", "true" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("APISERVICE command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Cache command for microservice scenario.
    /// </summary>
    [CommandRegister("CACHE", "Cache service command")]
    public class CacheServiceCommand : ICommandDelegate
    {
        public string Command => "CACHE";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "REDIS_URL", "redis://localhost:6379" },
                { "TTL", "3600" },
                { "MAX_MEMORY", "256mb" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("CACHE command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Queue command for microservice scenario.
    /// </summary>
    [CommandRegister("QUEUE", "Queue service command")]
    public class QueueServiceCommand : ICommandDelegate
    {
        public string Command => "QUEUE";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BROKER_URL", "amqp://localhost:5672" },
                { "QUEUE_NAME", "tasks" },
                { "PREFETCH_COUNT", "10" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("QUEUE command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command for consistency testing.
    /// </summary>
    [CommandRegister("TESTCONSISTENT", "Test consistency command")]
    public class TestConsistentCommand : ICommandDelegate
    {
        public string Command => "TESTCONSISTENT";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "VALUE1", "test1" },
                { "VALUE2", "test2" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("TESTCONSISTENT command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Config command for numeric/boolean test.
    /// </summary>
    [CommandRegister("CONFIG", "Config command")]
    public class ConfigTestCommand : ICommandDelegate
    {
        public string Command => "CONFIG";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "PORT", "8080" },
                { "ENABLED", "true" },
                { "RETRY_COUNT", "5" },
                { "THRESHOLD", "0.95" },
                { "DEBUG", "false" }
            };
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("CONFIG command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that has no default environment variables.
    /// </summary>
    [CommandRegister("NOENV", "Command with no default environment")]
    public class NoEnvironmentTestCommand : ICommandDelegate
    {
        public string Command => "NOENV";
        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext io, IEnvironmentContext environment)
        {
            yield return CommandResult<string>.Success("NoEnv command executed");
            await Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
