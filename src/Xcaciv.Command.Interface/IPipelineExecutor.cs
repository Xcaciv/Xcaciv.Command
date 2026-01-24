using System;
using System.Threading.Tasks;
using Xcaciv.Command;

namespace Xcaciv.Command.Interface;

/// <summary>
/// Executes piped command sequences with bounded channels and backpressure controls.
/// </summary>
public interface IPipelineExecutor
{
    /// <summary>
    /// Gets or sets the configuration applied when orchestrating pipeline execution.
    /// </summary>
    PipelineConfiguration Configuration { get; set; }
    string LastStageCommand { get; }

    /// <summary>
    /// Executes a pipeline string by invoking the supplied command delegate for each stage.
    /// </summary>
    /// <param name="commandLine">Full command line containing pipeline segments.</param>
    /// <param name="ioContext">Primary IO context for the pipeline.</param>
    /// <param name="environmentContext">Shared environment context.</param>
    /// <param name="executeCommand">Delegate that runs a single command.</param>
    Task ExecuteAsync(
    string commandLine,
    IIoContext ioContext,
    IControllerEnvironmentContext environmentContext,
    Func<string, IIoContext, IEnvironmentContext, Task> executeCommand);

    /// <summary>
    /// Executes a pipeline with cancellation support.
    /// </summary>
    Task ExecuteAsync(
        string commandLine,
        IIoContext ioContext,
        IControllerEnvironmentContext environmentContext,
        Func<string, IIoContext, IEnvironmentContext, CancellationToken, Task> executeCommand,
        CancellationToken cancellationToken);
}
