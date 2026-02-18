using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface
{
    /// <summary>
    /// Manages environment variables and context isolation for command execution.
    /// Commands execute in isolated child contexts; parent environment is updated
    /// only if the command has ModifiesEnvironment=true.
    /// </summary>
    /// <remarks>
    /// The environment context provides:
    /// - Case-insensitive environment variable storage and retrieval
    /// - Child context creation for command isolation
    /// - Change tracking (HasChanged property)
    /// - Audit logging integration
    /// 
    /// Security: Child contexts are isolated from parent contexts.
    /// A command cannot modify the parent environment unless explicitly allowed
    /// via the ModifiesEnvironment flag on its CommandDescription.
    /// </remarks>
    public interface IControllerEnvironmentContext : ICommandContext<IControllerEnvironmentContext>
    {
        /// <summary>
        /// Sets an environment variable to the specified value.
        /// </summary>
        /// <param name="key">The variable name (case-insensitive; stored as uppercase).</param>
        /// <param name="value">The variable value.</param>
        /// <remarks>
        /// If the variable already exists, its value is overwritten.
        /// This operation marks the context as HasChanged = true.
        /// If an audit logger is configured, the change is logged.
        /// </remarks>
        void SetValue(string key, string value, string commandName);

        /// <summary>
        /// create a child environment context
        /// </summary>
        /// <param name="commandName">index for values</param>
        /// <returns>IEnvironmentContext for command</returns>
        Task<IEnvironmentContext> GetChild(string commandName);

        /// <summary>
        /// Retrieves all environment variables as a dictionary.
        /// </summary>
        /// <returns>A new dictionary containing all current environment variables.</returns>
        /// <remarks>
        /// Returns a snapshot of the current environment. Modifications to the returned
        /// dictionary do not affect the environment context; use SetValue() to modify variables.
        /// </remarks>
        Dictionary<string, string> GetEnvironment();

        /// <summary>
        /// Retrieves all environment variables as a dictionary.
        /// </summary>
        /// <returns>A new dictionary containing all current environment variables.</returns>
        /// <remarks>
        /// Returns a snapshot of the current environment. Modifications to the returned
        /// dictionary do not affect the environment context; use SetValue() to modify variables.
        /// </remarks>
        Dictionary<string, string> GetEnvironment(string commandName, bool prefix = true);

        /// <summary>
        /// Indicates whether this context has modified any environment variables.
        /// </summary>
        /// <value>true if any variables have been added or changed; false if unchanged.</value>
        /// <remarks>
        /// Used by the framework to determine whether to update parent environment
        /// after command execution (if ModifiesEnvironment=true).
        /// </remarks>
        bool HasChanged { get; }

        /// <summary>
        /// Synchronizes environment variables from another environment or dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary of variables to merge into this context.</param>
        /// <remarks>
        /// Used by the framework to update the parent environment after a command
        /// execution (if the command has ModifiesEnvironment=true).
        /// Overwrites any existing variables with matching keys.
        /// </remarks>
        void UpdateEnvironment(Dictionary<string, string> dictionary, string commandName);

        /// <summary>
        /// Synchronizes environment variables from another environment or dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary of variables to merge into this context.</param>
        /// <remarks>
        /// Used by the framework to update the parent environment after a command
        /// execution (if the command has ModifiesEnvironment=true).
        /// Overwrites any existing variables with matching keys.
        /// </remarks>
        void UpdateEnvironment(Dictionary<string, string> dictionary);

        /// <summary>
        /// Set the audit logger for this environment context
        /// </summary>
        void SetAuditLogger(IAuditLogger auditLogger);

        /// <summary>
        /// return all command environment names
        /// </summary>
        /// <returns>A list of strings containing the names of all registered command environments.</returns>
        List<string> GetCommandEnvironmentNames();
    }
}
