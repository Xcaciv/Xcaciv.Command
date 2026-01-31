using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command
{
    public class ControllerEnvironmentContext : IControllerEnvironmentContext
    {
        /// <summary>
        /// Thread safe collection of env vars
        /// MUST be set when creating a child!
        /// </summary>
        protected ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _commandEnvironment { get; set; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// </summary>
        protected IEnvironmentContext _environment { get; set; } = new EnvironmentContext();
        /// <summary>
        /// Gets or sets a value indicating whether the current object or its associated environment has been modified
        /// since the last save operation.
        /// </summary>
        /// <remarks>Use this property to determine if changes have occurred that may require saving or
        /// updating the object's state. Setting this property to <see langword="false"/> can be used to reset the
        /// change tracking after a save operation.</remarks>
        public bool HasChanged 
        { 
            get
            {
                return field || _environment.HasChanged;
            }
            set; 
        }
        /// <summary>
        /// Gets the unique identifier for the entity.
        /// </summary>
        /// <remarks>The Id property is automatically initialized with a new GUID when the entity is
        /// created. This ensures that each instance has a distinct identifier.</remarks>
        public Guid Id { get; } = Guid.NewGuid();
        /// <summary>
        /// Gets or sets the name of the controller environment.
        /// </summary>
        public string Name {  get; set; } = "Controller Envirnonment";
        /// <summary>
        /// Gets or sets the unique identifier of the parent element, if any.
        /// </summary>
        /// <remarks>This property can be null, indicating that the current element has no parent. It is
        /// typically used to establish a hierarchical relationship between elements.</remarks>
        public Guid? Parent { get; set; }
        /// <summary>
        /// Initializes a new instance of the ControllerEnvironmentContext class.
        /// </summary>
        public ControllerEnvironmentContext() { }
        /// <summary>
        /// Initializes a new instance of the ControllerEnvironmentContext class using the specified environment
        /// context.
        /// </summary>
        /// <param name="environment">The environment context that provides information about the current environment. This parameter cannot be
        /// null.</param>
        public ControllerEnvironmentContext(IEnvironmentContext environment) 
        {
            _environment = environment;
        }
        /// <summary>
        /// Initializes a new instance of the ControllerEnvironmentContext class with the specified environment context
        /// and command-specific environment settings.
        /// </summary>
        /// <remarks>Use this constructor to create a ControllerEnvironmentContext that supports
        /// concurrent management of environment settings across multiple commands. The commandEnvironment parameter
        /// enables safe sharing and updating of command-specific settings in multi-threaded scenarios.</remarks>
        /// <param name="environment">The environment context that provides access to the current environment's settings and configurations.</param>
        /// <param name="commandEnvironment">A thread-safe dictionary containing environment settings for individual commands, allowing concurrent access
        /// and modification.</param>
        public ControllerEnvironmentContext(IEnvironmentContext environment, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> commandEnvironment)
        {
            _environment = environment;
            _commandEnvironment = commandEnvironment;
        }
        /// <summary>
        /// Asynchronously releases all resources used by the current instance.
        /// </summary>
        /// <remarks>Call this method when the object is no longer needed to ensure that all resources are
        /// released properly. This method should be awaited to guarantee that resource cleanup is complete before
        /// continuing execution.</remarks>
        /// <returns>A completed <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
        /// <summary>
        /// Asynchronously retrieves a context representing the child environment and its associated command
        /// environments.
        /// </summary>
        /// <remarks>The returned context includes a new child environment and a concurrent dictionary of
        /// command environments that is case-insensitive. Use this method when you need to operate within a child
        /// environment context derived from the current environment.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see
        /// cref="IControllerEnvironmentContext"/> instance for the child environment, including a case-insensitive copy
        /// of the current command environments.</returns>
        public async Task<IControllerEnvironmentContext> GetChild()
        {
            var childEnv = await _environment.GetChild().ConfigureAwait(false);
            var childCommandEnv = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(_commandEnvironment, StringComparer.OrdinalIgnoreCase);
            return new ControllerEnvironmentContext(childEnv, childCommandEnv);
        }
        /// <summary>
        /// Asynchronously retrieves a child environment context with command-specific key prefixes applied.
        /// </summary>
        /// <remarks>This method obtains a child environment context and updates its keys by prepending
        /// the specified command name followed by an underscore. This ensures that all keys in the child context are
        /// uniquely associated with the given command, which can help prevent key collisions when managing multiple
        /// command environments.</remarks>
        /// <param name="commandName">The name of the command used to generate a prefix for the keys in the child environment context. Cannot be
        /// null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an instance of
        /// IEnvironmentContext with keys prefixed by the specified command name.</returns>
        public async Task<IEnvironmentContext> GetChild(string commandName)
        {
            var child = await _environment.GetChild().ConfigureAwait(false);
            var commandEnv = GetEnvironment(commandName);

            // apply command prfix to command keys
            var commandPrefix = string.Concat(commandName, "_");
            foreach (var (key, value) in commandEnv)
            {
                var newKey = key.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase) ? key : commandPrefix + key;
                child.SetValue(newKey, value);
            }

            return child;
        }
        /// <summary>
        /// Gets the current environment variables as a dictionary of key-value pairs.
        /// </summary>
        /// <returns>A dictionary containing the environment variables, where each key is the variable name and each value is the
        /// corresponding variable value.</returns>
        public Dictionary<string, string> GetEnvironment()
        {
            return this._environment.GetEnvironment();
        }
        /// <summary>
        /// Retrieves the environment variables associated with the specified command name.
        /// </summary>
        /// <remarks>If the command name is not found, an empty dictionary is returned. This method allows
        /// for retrieving specific environment settings based on command context.</remarks>
        /// <param name="commandName">The name of the command for which to retrieve the environment variables. If null or empty, the default
        /// environment variables are returned.</param>
        /// <returns>A dictionary containing the environment variables for the specified command name. Returns an empty
        /// dictionary if the command name does not exist in the command environment.</returns>
        public Dictionary<string, string> GetEnvironment(string commandName)
        {
            if (String.IsNullOrEmpty(commandName))
            {
                return this._environment.GetEnvironment();
            }
            else
            {
                return _commandEnvironment.TryGetValue(commandName, out var commandEnv)
                    ? new Dictionary<string, string>(commandEnv)
                    : new Dictionary<string, string>();
            }
        }
        /// <summary>
        /// Sets the value associated with the specified key in the environment, optionally scoping the value to a
        /// specific command.
        /// </summary>
        /// <remarks>Use this method to manage environment values that may be shared globally or isolated
        /// per command. When a command name is specified and the key is appropriately prefixed, the value is stored in
        /// a command-specific context, allowing for command-level state management. If no command name is provided or
        /// the key does not match the expected prefix, the value is set globally.</remarks>
        /// <param name="key">The key for which the value is to be set. This key must be unique within the environment or command scope.</param>
        /// <param name="addValue">The value to associate with the specified key. This value replaces any existing value for the key.</param>
        /// <param name="commandName">An optional command name used to scope the key-value pair. If provided and the key is prefixed with the
        /// command name, the value is set in a command-specific environment; otherwise, it is set in the global
        /// environment.</param>
        public void SetValue(string key, string addValue, string commandName = "")
        {
            if (String.IsNullOrEmpty(commandName))
            {
                _environment.SetValue(key, addValue);
                return;
            }

            var commandPrefix = string.Concat(commandName, "_");
            if (!key.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _environment.SetValue(key, addValue);
                return;
            }

            string? oldValue = null;

            var commandEnvironment = _commandEnvironment.GetOrAdd(commandName, new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            commandEnvironment.AddOrUpdate(key, addValue, (commandKey, existingValue) =>
            {
                oldValue = existingValue;
                Trace.WriteLine($"CommandEnvironment [{commandName}] value {commandKey} changed from {existingValue} to {addValue}.");
                return addValue;
            });
            HasChanged = true;
        }
        /// <summary>
        /// Updates the environment with the specified key-value pairs.
        /// </summary>
        /// <remarks>This method applies the updates to the environment immediately. Ensure that the
        /// dictionary contains valid keys for the environment settings.</remarks>
        /// <param name="dictionary">A dictionary containing the environment settings to update. Each key represents a configuration setting, and
        /// its corresponding value specifies the new value to apply. Cannot be null.</param>
        public void UpdateEnvironment(Dictionary<string, string> dictionary)
        {
            _environment.UpdateEnvironment(dictionary);
        }
        /// <summary>
        /// Updates the environment with the specified key-value pairs, applying updates either globally or to a
        /// command-specific environment based on the provided command name.
        /// </summary>
        /// <remarks>If a command name is specified, only dictionary entries with keys that start with the
        /// command name followed by an underscore are applied to the command-specific environment; all other entries
        /// are applied to the shared environment. The HasChanged property is set to <see langword="true"/> if any
        /// command-specific updates are made.</remarks>
        /// <param name="dictionary">A dictionary containing key-value pairs to be applied to the environment. Keys prefixed with the command
        /// name and an underscore are treated as command-specific updates.</param>
        /// <param name="commandName">The name of the command for which command-specific environment updates should be applied. If null or empty,
        /// all updates are applied to the shared environment.</param>
        public void UpdateEnvironment(Dictionary<string, string> dictionary, string commandName)
        {
            if (String.IsNullOrEmpty(commandName))
            {
                _environment.UpdateEnvironment(dictionary);
                return;
            }

            var commandPrefix = string.Concat(commandName, "_");
            var sharedEnvironmentUpdates = new Dictionary<string, string>();
            var commandEnvironment = _commandEnvironment.GetOrAdd(commandName, new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            var commandEnvironmentUpdated = false;

            foreach ((var key, var addValue) in dictionary)
            {
                if (key.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string? oldValue = null;
                    commandEnvironment.AddOrUpdate(key, addValue, (environmentKey, existingValue) =>
                    {
                        oldValue = existingValue;
                        Trace.WriteLine($"CommandEnvironment [{commandName}] value {environmentKey} changed from {existingValue} to {addValue}.");
                        return addValue;
                    });
                    commandEnvironmentUpdated = true;
                }
                else
                {
                    sharedEnvironmentUpdates[key] = addValue;
                }
            }

            if (sharedEnvironmentUpdates.Count > 0)
            {
                _environment.UpdateEnvironment(sharedEnvironmentUpdates);
            }

            if (commandEnvironmentUpdated)
            {
                HasChanged = true;
            }
        }

        /// <summary>
        /// Set the audit logger for this environment context
        /// </summary>
        public virtual void SetAuditLogger(IAuditLogger auditLogger)
        {
            _environment.SetAuditLogger(auditLogger);
        }
        /// <summary>
        /// Retrieves a list of command environment names available in the current context.
        /// </summary>
        /// <returns>A list of strings containing the names of all command environments. The list will be empty if no command
        /// environments are defined.</returns>
        public List<string> GetCommandEnvironmentNames()
        {
            return _commandEnvironment.Keys.ToList();
        }
    }
}
