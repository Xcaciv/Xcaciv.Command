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
            child.UpdateEnvironment(GetEnvironment(commandName));
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
            }
            else
            {
                string? oldValue = null;

                // update CommandEnvironment with the key commandEnvKey, sub-dictionary key of 'key' trace the change
                var commandEnv = _commandEnvironment.GetOrAdd(commandName, new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                commandEnv.AddOrUpdate(key, addValue, (key, value) =>
                {
                    oldValue = value;
                    Trace.WriteLine($"CommandEnvironment [{commandName}] value {key} changed from {value} to {addValue}.");
                    return addValue;
                });
                HasChanged = true;
            }
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
            }
            else
            {
                var commandEnv = _commandEnvironment.GetOrAdd(commandName, new ConcurrentDictionary<string, string>());
                foreach ((var key, var addValue) in dictionary)
                {
                    string? oldValue = null;
                    commandEnv.AddOrUpdate(key, addValue, (key, value) =>
                    {
                        oldValue = value;
                        Trace.WriteLine($"CommandEnvironment [{commandName}] value {key} changed from {value} to {addValue}.");
                        return addValue;
                    });
                }
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
