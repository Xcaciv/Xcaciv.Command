using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface
{
    /// <summary>
    /// interface for commands that can be issued from a shell
    /// </summary>
    public interface ICommandDelegate : IAsyncDisposable
    {
        /// <summary>
        /// primary command execution method
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="messageContext">used for progress and status messages</param>
        /// <returns></returns>
        IAsyncEnumerable<string> Main(IIoContext input, IEnvironmentContext env);
        /// <summary>
        /// output usage instructions via message context
        /// </summary>
        /// <param name="messageContext"></param>
        /// <returns></returns>
        string Help(string[] parameters, IEnvironmentContext env);
        /// <summary>
        /// output single line help, used when listing all commands
        /// </summary>
        /// <param name="context"></param>
        string OneLineHelp(string[] parameters);
    }
}
 