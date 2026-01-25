using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command;

/// <summary>
/// Default pipeline executor that builds bounded channels between command stages.
/// </summary>
public class PipelineExecutor : IPipelineExecutor
{
    public PipelineConfiguration Configuration { get; set; } = new PipelineConfiguration();

    public string LastStageCommand { get; internal set; } = string.Empty;

    public PipelineExecutor()
    {
    }


    public async Task ExecuteAsync(
        string commandLine,
        IIoContext ioContext,
        IControllerEnvironmentContext environmentContext,
        Func<string, IIoContext, IEnvironmentContext, Task> executeCommand)
    {
        await ExecuteAsync(commandLine, ioContext, environmentContext,
            async (cmd, ctx, env, ct) => await executeCommand(cmd, ctx, env).ConfigureAwait(false),
            CancellationToken.None).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(
        string commandLine,
        IIoContext ioContext,
        IControllerEnvironmentContext environmentContext,
        Func<string, IIoContext, IEnvironmentContext, CancellationToken, Task> executeCommand,
        CancellationToken cancellationToken)
    {
        if (commandLine == null) throw new ArgumentNullException(nameof(commandLine));
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));
        if (environmentContext == null) throw new ArgumentNullException(nameof(environmentContext));
        if (executeCommand == null) throw new ArgumentNullException(nameof(executeCommand));

        cancellationToken.ThrowIfCancellationRequested();

        var (tasks, outputChannel, lastStageCommand) = await CreatePipelineStages(commandLine, ioContext, environmentContext, executeCommand, cancellationToken).ConfigureAwait(false);

        var allStagesTask = Task.WhenAll(tasks);
        var cancellationWaitTask = Task.Delay(Timeout.Infinite, cancellationToken);

        var completed = await Task.WhenAny(allStagesTask, cancellationWaitTask).ConfigureAwait(false);

        if (completed == cancellationWaitTask)
        {
            // Parent cancellation requested; surface OperationCanceledException to caller
            throw new OperationCanceledException(cancellationToken);
        }

        // Await to propagate any stage exceptions
        await allStagesTask.ConfigureAwait(false);

        // If parent cancellation was requested at any point, propagate now
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        await CollectPipelineOutput(outputChannel, ioContext, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(List<Task> tasks, Channel<IResult<string>>? outputChannel, string lastStageCommand)> CreatePipelineStages(
        string commandLine,
        IIoContext ioContext,
        IControllerEnvironmentContext environmentContext,
        Func<string, IIoContext, IEnvironmentContext, CancellationToken, Task> executeCommand,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        Channel<IResult<string>>? pipeChannel = null;

        // Use formal parser to handle quoted arguments, escapes, and delimiters
        var commands = PipelineParser.ParsePipeline(commandLine);
        var totalStages = commands.Length;
        var currentStage = 1;

        foreach (var command in commands)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var commandName = NamesValidator.GetValidCommandName(command);
            var args = NamesValidator.GetArgumentsFromCommandline(command);
            var childIoContext = await ioContext.GetChild().ConfigureAwait(false);
            await childIoContext.SetParameters(args).ConfigureAwait(false);

            // Set pipeline stage metadata for audit logging
            childIoContext.SetPipelineStage(currentStage, totalStages);

            if (pipeChannel != null)
            {
                childIoContext.SetInputPipe(pipeChannel.Reader);
            }

            pipeChannel = Channel.CreateBounded<IResult<string>>(new BoundedChannelOptions(Configuration.MaxChannelQueueSize)
            {
                FullMode = GetChannelFullMode(Configuration.BackpressureMode)
            });
            childIoContext.SetOutputPipe(pipeChannel.Writer);

            var childEnvironmentContext = await environmentContext.GetChild(commandName);
            tasks.Add(RunStageAsync(commandName, childIoContext, childEnvironmentContext, executeCommand, Configuration, cancellationToken));
            if (childEnvironmentContext.HasChanged)
            {
                environmentContext.UpdateEnvironment(childEnvironmentContext.GetEnvironment(), commandName);
            }

            // track last stage info for potential global reintegration
            LastStageCommand = commandName;

            currentStage++;
        }

        return (tasks, pipeChannel, LastStageCommand);
    }

    private Task RunStageAsync(
        string commandName,
        IIoContext childContext,
        IEnvironmentContext childEnvironmentContext,
        Func<string, IIoContext, IEnvironmentContext, CancellationToken, Task> executeCommand,
        PipelineConfiguration configuration,
        CancellationToken cancellationToken)
    {
        return RunStageInternal();

        async Task RunStageInternal()
        {
            await childContext.AddTraceMessage($"Pipeline stage start: {commandName}").ConfigureAwait(false);
            await using (childContext)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Create stage-specific timeout if configured
                using var stageCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (configuration.StageTimeoutSeconds > 0)
                {
                    stageCts.CancelAfter(TimeSpan.FromSeconds(configuration.StageTimeoutSeconds));
                }

                try
                {
                    await executeCommand(commandName, childContext, childEnvironmentContext, stageCts.Token).ConfigureAwait(false);

                    // If parent cancellation was requested during or right after execution, propagate it
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await childContext.AddTraceMessage($"Pipeline stage cancelled: {commandName}").ConfigureAwait(false);
                        // complete gracefully for downstream, then throw to propagate cancellation
                        await childContext.Complete(null).ConfigureAwait(false);
                        throw new OperationCanceledException(cancellationToken);
                    }

                    await childContext.Complete(null).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stageCts.Token.IsCancellationRequested)
                {
                    // Determine if this was a stage timeout or parent cancellation
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // Stage-specific cancellation (timeout if configured)
                        if (configuration.StageTimeoutSeconds > 0)
                        {
                            await childContext.AddTraceMessage($"Pipeline stage timeout: {commandName} (exceeded {configuration.StageTimeoutSeconds}s)").ConfigureAwait(false);
                            await childContext.Complete($"Stage '{commandName}' exceeded timeout of {configuration.StageTimeoutSeconds} seconds").ConfigureAwait(false);
                        }
                        else
                        {
                            // Stage cancelled but no timeout configured - treat as error
                            await childContext.AddTraceMessage($"Pipeline stage cancelled unexpectedly: {commandName}").ConfigureAwait(false);
                            await childContext.Complete($"Stage '{commandName}' was cancelled").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Parent cancellation - complete gracefully and propagate
                        await childContext.AddTraceMessage($"Pipeline stage cancelled: {commandName}").ConfigureAwait(false);
                        await childContext.Complete(null).ConfigureAwait(false);
                        throw;
                    }
                }
            }
        }
    }

    private async Task CollectPipelineOutput(Channel<IResult<string>>? outputChannel, IIoContext ioContext, CancellationToken cancellationToken)
    {
        if (outputChannel == null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await foreach (var output in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ioContext.OutputChunk(output).ConfigureAwait(false);
        }
    }

    private static BoundedChannelFullMode GetChannelFullMode(PipelineBackpressureMode mode) => mode switch
    {
        PipelineBackpressureMode.DropOldest => BoundedChannelFullMode.DropOldest,
        PipelineBackpressureMode.DropNewest => BoundedChannelFullMode.DropNewest,
        PipelineBackpressureMode.Block => BoundedChannelFullMode.Wait,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported backpressure mode")
    };

}
