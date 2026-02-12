using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Xcaciv.Command.FileLoaderTests;

/// <summary>
/// Unit tests for EnvironmentFileManager class.
/// Tests YAML serialization/deserialization, file handling, and error scenarios.
/// </summary>
public class EnvironmentFileManagerTests
{
    #region SaveEnvironment with IEnvironmentContext Tests

    [Fact]
    public void SaveEnvironment_WithIEnvironmentContext_CreatesYamlFile()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");  // Add directory
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();
        environment.SetValue("KEY1", "value1");
        environment.SetValue("KEY2", "value2");

        string filePath = @"C:\test\environment";

        // Act
        var result = manager.SaveEnvironment(filePath, environment);

        // Assert
        Assert.True(result);
        Assert.True(fileSystem.FileExists(@"C:\test\environment.yml"));
    }

    [Fact]
    public void SaveEnvironment_WithIEnvironmentContext_SavesCorrectYamlContent()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();
        environment.SetValue("USERNAME", "testuser");
        environment.SetValue("PATH", "/usr/bin");

        string filePath = @"C:\test\env.yaml";

        // Act
        manager.SaveEnvironment(filePath, environment);

        // Assert
        var content = fileSystem.File.ReadAllText(@"C:\test\env.yaml");
        Assert.Contains("global:", content);
        Assert.Contains("username: testuser", content.ToLower());
        Assert.Contains("path: /usr/bin", content.ToLower());
    }

    [Fact]
    public void SaveEnvironment_WithIEnvironmentContext_AddsYamlExtensionWhenMissing()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\config");
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();
        environment.SetValue("TEST", "value");

        string filePath = @"C:\config\myenv";

        // Act
        manager.SaveEnvironment(filePath, environment);

        // Assert
        Assert.True(fileSystem.FileExists(@"C:\config\myenv.yml"));
        Assert.False(fileSystem.FileExists(@"C:\config\myenv"));
    }

    [Fact]
    public void SaveEnvironment_WithIEnvironmentContext_PreservesYmlExtension()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\config");
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();
        environment.SetValue("TEST", "value");

        string filePath = @"C:\config\myenv.yml";

        // Act
        manager.SaveEnvironment(filePath, environment);

        // Assert
        Assert.True(fileSystem.FileExists(@"C:\config\myenv.yml"));
    }

    [Fact]
    public void SaveEnvironment_WithIEnvironmentContext_EmptyEnvironment_CreatesValidYaml()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();

        string filePath = @"C:\test\empty.yaml";

        // Act
        var result = manager.SaveEnvironment(filePath, environment);

        // Assert
        Assert.True(result);
        var content = fileSystem.File.ReadAllText(@"C:\test\empty.yaml");
        Assert.NotEmpty(content);
        Assert.Contains("global:", content);
    }

    #endregion

    #region SaveEnvironment with IControllerEnvironmentContext Tests

    [Fact]
    public void SaveEnvironment_WithIControllerEnvironmentContext_SavesGlobalAndCommandEnvironments()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var controllerEnv = new MockControllerEnvironmentContext();
        
        // Set global environment
        controllerEnv.SetGlobalEnvironment(new Dictionary<string, string>
        {
            { "GLOBAL_VAR", "global_value" }
        });
        
        // Set command environments
        controllerEnv.SetCommandEnvironment("command1", new Dictionary<string, string>
        {
            { "CMD1_VAR", "cmd1_value" }
        });
        controllerEnv.SetCommandEnvironment("command2", new Dictionary<string, string>
        {
            { "CMD2_VAR", "cmd2_value" }
        });

        string filePath = @"C:\test\controller_env.yaml";

        // Act
        var result = manager.SaveEnvironment(filePath, controllerEnv);

        // Assert
        Assert.True(result);
        var content = fileSystem.File.ReadAllText(@"C:\test\controller_env.yaml");
        Assert.Contains("global:", content);
        Assert.Contains("commandEnvironments:", content);
        Assert.Contains("command1:", content);
        Assert.Contains("command2:", content);
    }

    [Fact]
    public void SaveEnvironment_WithIControllerEnvironmentContext_MultipleCommands_SavesCorrectly()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var controllerEnv = new MockControllerEnvironmentContext();
        
        controllerEnv.SetGlobalEnvironment(new Dictionary<string, string>
        {
            { "USER", "admin" },
            { "HOME", "/home/admin" }
        });
        
        controllerEnv.SetCommandEnvironment("git", new Dictionary<string, string>
        {
            { "GIT_AUTHOR", "John Doe" }
        });
        
        controllerEnv.SetCommandEnvironment("docker", new Dictionary<string, string>
        {
            { "DOCKER_HOST", "tcp://localhost:2375" }
        });

        string filePath = @"C:\test\multi_cmd.yaml";

        // Act
        manager.SaveEnvironment(filePath, controllerEnv);

        // Assert
        var content = fileSystem.File.ReadAllText(@"C:\test\multi_cmd.yaml");
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var envFile = deserializer.Deserialize<EnvironmentFile>(content);
        
        Assert.Equal(2, envFile.Global.Count);
        Assert.Equal(2, envFile.CommandEnvironments.Count);
        Assert.True(envFile.CommandEnvironments.ContainsKey("git"));
        Assert.True(envFile.CommandEnvironments.ContainsKey("docker"));
    }

    [Fact]
    public void SaveEnvironment_WithIControllerEnvironmentContext_NoCommandEnvironments_OnlySavesGlobal()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var controllerEnv = new MockControllerEnvironmentContext();

        controllerEnv.SetGlobalEnvironment(new Dictionary<string, string>
        {
            { "ONLY_GLOBAL", "value" }
        });
        string filePath = @"C:\test\global_only.yaml";

        // Act
        manager.SaveEnvironment(filePath, controllerEnv);

        // Assert
        var content = fileSystem.File.ReadAllText(@"C:\test\global_only.yaml");
        Assert.Contains("global:", content);
        Assert.Contains("commandEnvironments:", content);

    }
    #endregion

    #region LoadEnvironmentFile Tests
    [Fact]
    public void LoadEnvironmentFile_ValidYamlFile_ReturnsEnvironmentFile()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var yamlContent = @"
global:
  VAR1: value1
  VAR2: value2
commandEnvironments:
  cmd1:
  cmd1:
    CMD_VAR: cmd_value
";
        fileSystem.AddFile(@"C:\test\load.yaml", new MockFileData(yamlContent));
        var manager = new EnvironmentFileManager(fileSystem);

        // Act
        var envFile = manager.LoadEnvironmentFile(@"C:\test\load.yaml");
        
        // Assert
        Assert.NotNull(envFile);
        Assert.Equal(2, envFile.Global.Count);
        Assert.Equal("value1", envFile.Global["VAR1"]);
        Assert.Equal("value2", envFile.Global["VAR2"]);
        Assert.Single(envFile.CommandEnvironments);
    }
    
    [Fact]
    public void LoadEnvironmentFile_AddsYmlExtensionWhenMissing()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var yamlContent = @"
global:
  VAR1: value1
commandEnvironments:
  cmd1:
  cmd1:
    CMD_VAR: cmd_value
";
        fileSystem.AddFile(@"C:\test\load.yml", new MockFileData(yamlContent));
        var manager = new EnvironmentFileManager(fileSystem);

        // Act - specify path without extension
        var envFile = manager.LoadEnvironmentFile(@"C:\test\load");

        // Assert
        Assert.NotNull(envFile);
        Assert.Single(envFile.Global);
    }

    [Fact]
    public void LoadEnvironmentFile_SupportsYmlExtension()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var yamlContent = @"
global:
  VAR1: value1
commandEnvironments:
  cmd1:
  cmd1:
    CMD_VAR: cmd_value
";
        fileSystem.AddFile(@"C:\test\config.yml", new MockFileData(yamlContent));
        var manager = new EnvironmentFileManager(fileSystem);

        // Act
        var envFile = manager.LoadEnvironmentFile(@"C:\test\config.yml");

        // Assert
        Assert.NotNull(envFile);
        Assert.Single(envFile.Global);
    }

    [Fact]
    public void LoadEnvironmentFile_EmptyYamlFile_ReturnsEmptyEnvironmentFile()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var yamlContent = @"
global: {}
commandEnvironments: {}
";
        fileSystem.AddFile(@"C:\test\empty.yaml", new MockFileData(yamlContent));
        
        var manager = new EnvironmentFileManager(fileSystem);

        // Act
        var envFile = manager.LoadEnvironmentFile(@"C:\test\empty.yaml");
        // Assert
        // Assert
        Assert.NotNull(envFile);
        Assert.Empty(envFile.Global);
        Assert.Empty(envFile.CommandEnvironments);
    }

    [Fact]
    public void LoadEnvironmentFile_FileNotFound_ThrowsException()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var manager = new EnvironmentFileManager(fileSystem);

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => 
            manager.LoadEnvironmentFile(@"C:\test\nonexistent.yaml"));
        
        Assert.Contains("Failed to load environment from file", exception.Message);
        Assert.Contains("nonexistent.yaml", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void LoadEnvironmentFile_InvalidYaml_ThrowsException()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var invalidYaml = @"
global: [this is not valid yaml syntax
global: [this is not valid yaml syntax
  KEY: value
";
        fileSystem.AddFile(@"C:\test\invalid.yaml", new MockFileData(invalidYaml));
        
        var manager = new EnvironmentFileManager(fileSystem);

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => 
            manager.LoadEnvironmentFile(@"C:\test\invalid.yaml"));
        
        Assert.Contains("Failed to load environment from file", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void LoadEnvironmentFile_ComplexEnvironment_PreservesAllData()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var yamlContent = @"
global:
  PATH: /usr/bin:/usr/local/bin
  USER: testuser
  HOME: /home/testuser
commandEnvironments:
  git:
    GIT_AUTHOR_NAME: John Doe
    GIT_AUTHOR_EMAIL: john@example.com
  docker:
    DOCKER_HOST: tcp://localhost:2375
    DOCKER_TLS_VERIFY: '1'
  npm:
    NPM_TOKEN: secret-token
";
        fileSystem.AddFile(@"C:\test\complex.yaml", new MockFileData(yamlContent));
        
        var manager = new EnvironmentFileManager(fileSystem);

        // Act
        var envFile = manager.LoadEnvironmentFile(@"C:\test\complex.yaml");

        // Assert
        Assert.Equal(3, envFile.Global.Count);
        Assert.Equal(3, envFile.CommandEnvironments.Count);
        Assert.Equal(2, envFile.CommandEnvironments["git"].Count);
        Assert.Equal(2, envFile.CommandEnvironments["docker"].Count);
        Assert.Single(envFile.CommandEnvironments["npm"]);
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var controllerEnv = new MockControllerEnvironmentContext();
        
        controllerEnv.SetGlobalEnvironment(new Dictionary<string, string>
        {
            { "VAR1", "value1" },
            { "VAR2", "value2" }
        });
        
        controllerEnv.SetCommandEnvironment("cmd1", new Dictionary<string, string>
        {
            { "CMD_VAR", "cmd_value" }
        });

        string filePath = @"C:\test\roundtrip.yaml";

        // Act - Save
        manager.SaveEnvironment(filePath, controllerEnv);
        
        // Act - Load
        var loadedEnvFile = manager.LoadEnvironmentFile(filePath);

        // Assert
        Assert.Equal(2, loadedEnvFile.Global.Count);
        Assert.Equal("value1", loadedEnvFile.Global["VAR1"]);
        Assert.Equal("value2", loadedEnvFile.Global["VAR2"]);
        Assert.Single(loadedEnvFile.CommandEnvironments);
        Assert.Equal("cmd_value", loadedEnvFile.CommandEnvironments["cmd1"]["CMD_VAR"]);
    }

    [Fact]
    public void SaveAndLoad_SpecialCharacters_PreservesCorrectly()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        // add directory to avoid file not found error
        fileSystem.AddDirectory(@"C:\test");
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();
        
        environment.SetValue("PATH", @"C:\Program Files\Git;C:\Windows\System32");
        environment.SetValue("MULTILINE", "line1\nline2\nline3");
        environment.SetValue("QUOTES", "value with \"quotes\" inside");

        string filePath = @"C:\test\special.yaml";

        // Act
        manager.SaveEnvironment(filePath, environment);
        var loadedEnvFile = manager.LoadEnvironmentFile(filePath);

        // Assert
        Assert.Equal(@"C:\Program Files\Git;C:\Windows\System32", loadedEnvFile.Global["PATH"]);
        Assert.Equal("line1\nline2\nline3", loadedEnvFile.Global["MULTILINE"]);
        Assert.Equal("value with \"quotes\" inside", loadedEnvFile.Global["QUOTES"]);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void SaveEnvironment_IEnvironmentContext_SaveFailure_ThrowsException()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var manager = new EnvironmentFileManager(fileSystem);
        var environment = new MockEnvironmentContext();
        environment.SetValue("TEST", "value");

        // Simulate a path that will fail
        string filePath = @"Z:\nonexistent\path\file.yaml";

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => 
            manager.SaveEnvironment(filePath, environment));
        
        Assert.Contains("Failed to save environment to file", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void SaveEnvironment_IControllerEnvironmentContext_SaveFailure_ThrowsException()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var manager = new EnvironmentFileManager(fileSystem);
        var controllerEnv = new MockControllerEnvironmentContext();
        
        controllerEnv.SetGlobalEnvironment(new Dictionary<string, string>
        {
            { "TEST", "value" }
        });

        string filePath = @"Z:\nonexistent\path\file.yaml";

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => 
            manager.SaveEnvironment(filePath, controllerEnv));
        
        Assert.Contains("Failed to save environment to file", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    #endregion

    #region Mock Classes

    private class MockEnvironmentContext : IEnvironmentContext
    {
        private readonly Dictionary<string, string> _environment = new();
        
        public bool HasChanged { get; private set; }
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "MockEnvironment";
        public Guid? Parent { get; set; }

        public void SetValue(string key, string value)
        {
            _environment[key.ToUpperInvariant()] = value;
            HasChanged = true;
        }

        public string GetValue(string key, string defaultValue = "", bool storeDefault = true)
        {
            var upperKey = key.ToUpperInvariant();
            if (_environment.TryGetValue(upperKey, out var value))
            {
                return value;
            }
            
            if (storeDefault)
            {
                _environment[upperKey] = defaultValue;
            }
            
            return defaultValue;
        }

        public Dictionary<string, string> GetEnvironment()
        {
            return new Dictionary<string, string>(_environment);
        }

        public void UpdateEnvironment(Dictionary<string, string> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                _environment[kvp.Key.ToUpperInvariant()] = kvp.Value;
            }
            HasChanged = true;
        }

        public void SetAuditLogger(IAuditLogger auditLogger)
        {
            // Not needed for tests
        }

        public IEnvironmentContext CreateChild()
        {
            var child = new MockEnvironmentContext
            {
                Parent = this.Id
            };
            child.UpdateEnvironment(_environment);
            return child;
        }

        public Task<IEnvironmentContext> GetChild()
        {
            return Task.FromResult(CreateChild());
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private class MockControllerEnvironmentContext : IControllerEnvironmentContext
    {
        private readonly Dictionary<string, string> _globalEnvironment = new();
        private readonly Dictionary<string, Dictionary<string, string>> _commandEnvironments = new();
        
        public bool HasChanged { get; private set; }
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "MockControllerEnvironment";
        public Guid? Parent { get; set; }

        public void SetGlobalEnvironment(Dictionary<string, string> env)
        {
            _globalEnvironment.Clear();
            foreach (var kvp in env)
            {
                _globalEnvironment[kvp.Key.ToUpperInvariant()] = kvp.Value;
            }
            HasChanged = true;
        }

        public void SetCommandEnvironment(string commandName, Dictionary<string, string> env)
        {
            _commandEnvironments[commandName] = new Dictionary<string, string>(env);
            HasChanged = true;
        }

        public void SetValue(string key, string value, string commandName)
        {
            if (!_commandEnvironments.ContainsKey(commandName))
            {
                _commandEnvironments[commandName] = new Dictionary<string, string>();
            }
            _commandEnvironments[commandName][key.ToUpperInvariant()] = value;
            HasChanged = true;
        }

        public Task<IEnvironmentContext> GetChild(string commandName)
        {
            var child = new MockEnvironmentContext();
            if (_commandEnvironments.TryGetValue(commandName, out var env))
            {
                child.UpdateEnvironment(env);
            }
            return Task.FromResult<IEnvironmentContext>(child);
        }

        public Task<IControllerEnvironmentContext> GetChild()
        {
            var child = new MockControllerEnvironmentContext
            {
                Parent = this.Id
            };
            child.SetGlobalEnvironment(_globalEnvironment);
            return Task.FromResult<IControllerEnvironmentContext>(child);
        }

        public Dictionary<string, string> GetEnvironment()
        {
            return new Dictionary<string, string>(_globalEnvironment);
        }

        public Dictionary<string, string> GetEnvironment(string commandName)
        {
            return _commandEnvironments.TryGetValue(commandName, out var env) 
                ? new Dictionary<string, string>(env) 
                : new Dictionary<string, string>();
        }

        public void UpdateEnvironment(Dictionary<string, string> dictionary, string commandName)
        {
            if (!_commandEnvironments.ContainsKey(commandName))
            {
                _commandEnvironments[commandName] = new Dictionary<string, string>();
            }
            
            foreach (var kvp in dictionary)
            {
                _commandEnvironments[commandName][kvp.Key.ToUpperInvariant()] = kvp.Value;
            }
            HasChanged = true;
        }

        public void UpdateEnvironment(Dictionary<string, string> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                _globalEnvironment[kvp.Key.ToUpperInvariant()] = kvp.Value;
            }
            HasChanged = true;
        }

        public void SetAuditLogger(IAuditLogger auditLogger)
        {
            // Not needed for tests
        }

        public List<string> GetCommandEnvironmentNames()
        {
            return _commandEnvironments.Keys.ToList();
        }

        public IControllerEnvironmentContext CreateChild()
        {
            var child = new MockControllerEnvironmentContext
            {
                Parent = this.Id
            };
            child.SetGlobalEnvironment(_globalEnvironment);
            return child;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    #endregion
}
