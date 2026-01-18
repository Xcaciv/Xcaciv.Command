using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface
{
    public interface ICommandContext<T>: IAsyncDisposable
    {
        /// <summary>
        /// current message context identifier
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// friendly name for user output
        /// </summary>
        string Name { get; }
        /// <summary>
        /// parent context identifier if there is one
        /// </summary>
        Guid? Parent { get; }
        /// <summary>
        /// create a child output context
        /// MUST pass down expected Environment values
        /// may track the instance for later use
        /// </summary>
        /// <param name="childArguments">arguments to pass to child context</param>
        /// <param name="pipeline">specify we are dealing with a pipeline</param>
        /// <returns></returns>
        Task<T> GetChild();
    }
}
