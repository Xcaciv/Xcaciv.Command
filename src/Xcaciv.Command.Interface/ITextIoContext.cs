using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface;

/// <summary>
/// thread safe string pump for UI syncronization context
/// This interface is used to abstract the UI from the command processor
/// </summary>
public interface ITextIoContext
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
    /// get command text from context
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    Task<string> PromptForCommand(string prompt);
    /// <summary>
    /// output text WITHOUT new line
    /// used for cumulative standard output
    /// SHOULD NOT CONTAIN ACTUAL RESULT
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task OutputChunk(string message);
    /// <summary>
    /// output text ending in new line
    /// used for cumulative output
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task OutputLine(string message);
    /// <summary>
    /// replace status text with new text
    /// used for static status output
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    Task SetStatusMessage(string message);
    /// <summary>
    /// set proces progress based on total
    /// </summary>
    /// <param name="total"></param>
    /// <param name="step"></param>
    /// <returns>whole number signifying percentage</returns>
    Task<int> SetProgress(int total, int step);
    /// <summary>
    /// create a child output context
    /// may track the instance for later use
    /// </summary>
    /// <param name="Name">friendly name for user output</param>
    /// <returns></returns>
    Task<ITextIoContext> GetChild(string Name);
}
