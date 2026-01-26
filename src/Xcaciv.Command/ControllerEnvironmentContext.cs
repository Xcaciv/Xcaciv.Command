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

        public bool HasChanged 
        { 
            get
            {
                return field || _environment.HasChanged;
            }
            set; 
        }

        public Guid Id { get; } = Guid.NewGuid();

        public string Name {  get; set; } = "Controller Envirnonment";

        public Guid? Parent { get; set; }

        public ControllerEnvironmentContext() { }

        public ControllerEnvironmentContext(IEnvironmentContext environment) 
        {
            _environment = environment;
        }


        public ControllerEnvironmentContext(IEnvironmentContext environment, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> commandEnvironment)
        {
            _environment = environment;
            _commandEnvironment = commandEnvironment;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async Task<IControllerEnvironmentContext> GetChild()
        {
            var childEnv = await _environment.GetChild().ConfigureAwait(false);
            var childCommandEnv = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(_commandEnvironment, StringComparer.OrdinalIgnoreCase);
            return new ControllerEnvironmentContext(childEnv, childCommandEnv);
        }

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
        public Dictionary<string, string> GetEnvironment()
        {
            return this._environment.GetEnvironment();
        }
        public Dictionary<string, string> GetEnvironment(string commandName)
        {
            if (String.IsNullOrEmpty(commandName))
            {
                return this._environment.GetEnvironment();
            }
            else
            {
                if (_commandEnvironment.TryGetValue(commandName, out var commandEnv))
                {
                    return new Dictionary<string, string>(commandEnv);
                }
                else
                {
                    return new Dictionary<string, string>();
                }
            }
        }

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
        public void UpdateEnvironment(Dictionary<string, string> dictionary)
        {
            _environment.UpdateEnvironment(dictionary);
        }
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
    }
}
