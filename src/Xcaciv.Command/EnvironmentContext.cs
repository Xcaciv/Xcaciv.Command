using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command
{
    public class EnvironmentContext : IEnvironmentContext
    {
        /// <summary>
        /// Thread safe collection of env vars
        /// MUST be set when creating a child!
        /// </summary>
        protected ConcurrentDictionary<string, string> EnvironmentVariables { get; set; } = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Optional audit logger for environment variable changes
        /// </summary>
        protected IAuditLogger? _auditLogger;

        public bool HasChanged {  get; private set; }

        public Guid Id {  get; } = Guid.NewGuid();

        public string Name { get; set; } = "Environment";

        public Guid? Parent {  get; protected set; }

        public EnvironmentContext() { }

        public EnvironmentContext(Dictionary<string, string> environment)
        {
            this.UpdateEnvironment(environment);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task<IEnvironmentContext> GetChild(string[]? childArguments = null)
        {
            var childValues = this.GetEnvinronment();
            var child = new EnvironmentContext(childValues);
            // Propagate audit logger to child
            if (_auditLogger != null)
            {
                child.SetAuditLogger(_auditLogger);
            }
            return Task.FromResult<IEnvironmentContext>(child);
        }

        /// <summary>
        /// Set the audit logger for this environment context
        /// </summary>
        public virtual void SetAuditLogger(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }
        
        /// <summary>
        /// <see cref="Xcaciv.Command.Interface.IEnvironmentContext"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="addValue"></param>
        /// <returns></returns>
        public virtual void SetValue(string key, string addValue)
        {
            // make case insensitive var names
            key = key.ToUpper();
            string? oldValue = null;
            
            EnvironmentVariables.AddOrUpdate(key, addValue, (existingKey, existingValue) =>
            {
                oldValue = existingValue;
                Trace.WriteLine($"Environment value {key} changed from {existingValue} to {addValue}.");
                return addValue;
            });

            this.HasChanged = true;

            // Log environment change to audit trail
            _auditLogger?.LogEnvironmentChange(key, oldValue, addValue, "system", DateTime.UtcNow);
        }
        /// <summary>
        /// <see cref="Xcaciv.Command.Interface.IEnvironmentContext"/>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual string GetValue(string key, string defaultValue = "", bool storeDefault = true)
        {
            // make case insensitive var names
            key = key.ToUpper();

            string? returnValue;
            if (!EnvironmentVariables.TryGetValue(key, out returnValue) &&
                storeDefault)
            {
                // store the default value if instructed
                this.SetValue(key, defaultValue);
                return defaultValue;
            }

            // return the value or the default
            return returnValue ?? defaultValue;
        }

        // todo GetValue with default and optional set env var

        public Dictionary<string, string> GetEnvinronment()
        {
            return EnvironmentVariables.ToDictionary();
        }

        public void UpdateEnvironment(Dictionary<string, string> dictionary)
        {
            foreach (var pair in dictionary)
            {
                SetValue(pair.Key, pair.Value);
            }
        }
    }
}
