using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Interface
{
    /// <summary>
    /// Defines the contract for a command that can be executed within the framework.
    /// All commands must implement this interface and be decorated with CommandRegisterAttribute.
    /// </summary>
    /// <remarks>
    /// Commands are discovered via attributes (CommandRegisterAttribute for registration,
    /// CommandRootAttribute for root-level sub-commands, and parameter attributes for
    /// parameter definitions). Commands support pipelining via IAsyncEnumerable output.
    /// 
    /// Security: Commands execute in isolated environments. Only commands with
    /// ModifiesEnvironment=true can alter environment variables.
    /// </remarks>
    public interface ICommandDelegate : IAsyncDisposable
    {
        /// <summary>
        /// Gets the command text associated with this instance.
        /// Must be alphanumeric and contain no spaces.
        /// </summary>
        string Command { get; }
        /// <summary>
        /// Gets the root command that this command is nested under, if any.
        /// Must be alphanumeric and contain no spaces.
        /// </summary>
        string RootCommand { get; }
        /// <summary>
        /// Primary command execution method.
        /// </summary>
        /// <param name="ioContext">The IO context for input/output and parameter access.</param>
        /// <param name="env">The environment context for variable access (isolated child context if environment-modifying).</param>
        /// <returns>An async enumerable of result objects. Successful results carry output chunks; failures describe errors.</returns>
        /// <remarks>
        /// Implementations should yield output via "yield return" or async enumeration.
        /// Output supports pipelining: if part of a piped command sequence, output is sent
        /// to the next command's input via channels.
        /// 
        /// The environment context passed is a child context isolated from the parent.
        /// Changes to this context are not reflected in the parent unless the command
        /// has ModifiesEnvironment=true on its CommandDescription.
        /// </remarks>
        IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env);

        /// <summary>
        /// Retrieves a dictionary containing the environment variables this command uses and their defalut values.
        /// </summary>
        /// <returns>A dictionary where each key is the name of an environment variable and each value is the corresponding
        /// value. The dictionary is empty if no environment variables are available.</returns>
        /// <remarks>
        /// This dictionary is required for this command to recieve additional settings. The values will be scoped in the global environment
        /// with the prefix of the command name. For example, this dictionary might return a key/value pair of ("TIMEOUT", "30"), and the command 
        /// is named "FETCH". The command will then be able to access the environment variable "TIMEOUT" with a default value of "30". In the 
        /// global environment, this variable will be stored as "FETCH_TIMEOUT".
        /// </remarks>
        Dictionary<string, string> GetDefaultEnvironment();

        /// <summary>
        /// Retrieves a list of parameters this command accepts.
        /// </summary>
        /// <returns>A list of <see cref="ICommandParameter"/> indicating the parameters for the command.</returns>
        List<ICommandParameter> GetParameters();
    }
}