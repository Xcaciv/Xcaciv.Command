using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Parameters;
using Xcaciv.Command.Tests.TestImpementations;
using Xcaciv.Command.Core;
using System.Linq;

namespace Xcaciv.Command.Tests;

public class PipelineChannelCompletionTests
{
    [Fact]
    public async Task PipelineCompletesChannels_WhenProducerAndConsumerFinishAsync()
    {
        var pipelineExecutor = new PipelineExecutor();
        var environmentContext = new ControllerEnvironmentContext(new EnvironmentContext());
        var rootContext = new TestTextIo();
        var consumerCompletions = 0;

        var commandFactories = new Dictionary<string, Func<ICommandDelegate>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PIPECHANNELPRODUCER"] = () => new PipelineChannelProducerCommand(),
            ["PIPECHANNELCONSUMER"] = () => new PipelineChannelConsumerCommand(() => Interlocked.Increment(ref consumerCompletions))
        };

        var runTask = pipelineExecutor.ExecuteAsync(
            "PIPECHANNELPRODUCER alpha beta | PIPECHANNELCONSUMER",
            rootContext,
            environmentContext,
            (name, ctx, env) => ExecuteCommandAsync(name, ctx, env, commandFactories));

        var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(completedTask == runTask, "Pipeline execution timed out before completion.");
        await runTask;

        Assert.Contains("alpha", rootContext.Output);
        Assert.Contains("beta", rootContext.Output);
        Assert.Equal(1, consumerCompletions);
    }

    [Fact]
    public async Task PipelineCompletesChannels_AcrossMultipleStagesAsync()
    {
        var pipelineExecutor = new PipelineExecutor();
        var environmentContext = new ControllerEnvironmentContext(new EnvironmentContext());
        var rootContext = new TestTextIo();
        var passThroughCompletions = 0;
        var consumerCompletions = 0;

        var commandFactories = new Dictionary<string, Func<ICommandDelegate>>(StringComparer.OrdinalIgnoreCase)
        {
            ["PIPECHANNELPRODUCER"] = () => new PipelineChannelProducerCommand(),
            ["PIPECHANNELPASSTHROUGH"] = () => new PipelineChannelPassThroughCommand(() => Interlocked.Increment(ref passThroughCompletions)),
            ["PIPECHANNELCONSUMER"] = () => new PipelineChannelConsumerCommand(() => Interlocked.Increment(ref consumerCompletions))
        };

        var runTask = pipelineExecutor.ExecuteAsync(
            "PIPECHANNELPRODUCER foo | PIPECHANNELPASSTHROUGH | PIPECHANNELCONSUMER",
            rootContext,
            environmentContext,
            (name, ctx, env) => ExecuteCommandAsync(name, ctx, env, commandFactories));

        var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(completedTask == runTask, "Pipeline execution timed out before completion.");
        await runTask;

        Assert.Contains("foo", rootContext.Output);
        Assert.Equal(1, passThroughCompletions);
        Assert.Equal(1, consumerCompletions);
    }

    private static async Task ExecuteCommandAsync(
        string commandName,
        IIoContext ioContext,
        IEnvironmentContext environmentContext,
        IDictionary<string, Func<ICommandDelegate>> commandFactories)
    {
        if (!commandFactories.TryGetValue(commandName, out var factory))
        {
            throw new InvalidOperationException($"Command [{commandName}] is not registered in the test harness.");
        }

        await using var command = factory();
        await foreach (var result in command.Main(ioContext, environmentContext))
        {
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Output))
            {
                await ioContext.OutputChunk(result).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Emits deterministic pipeline output - implements ICommandDelegate directly to have full control over Main
    /// </summary>
    private sealed class PipelineChannelProducerCommand : ICommandDelegate
    {
        public string Command => "PIPECHANNELPRODUCER";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            if (ioContext.Parameters.Length == 0)
            {
                yield return CommandResult<string>.Success("producer-default");
                yield break;
            }

            foreach (var value in ioContext.Parameters)
            {
                yield return CommandResult<string>.Success(value);
                await Task.Yield();
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Reads from input and forwards to output - implements ICommandDelegate directly
    /// </summary>
    private sealed class PipelineChannelPassThroughCommand : ICommandDelegate
    {
        private readonly Action? _onCompleted;

        public PipelineChannelPassThroughCommand(Action? onCompleted)
        {
            _onCompleted = onCompleted;
        }

        public string Command => "PIPECHANNELPASSTHROUGH";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            if (!ioContext.HasPipedInput)
            {
                _onCompleted?.Invoke();
                yield break;
            }

            await foreach (var chunk in ioContext.ReadInputPipeChunks())
            {
                yield return chunk;
            }

            _onCompleted?.Invoke();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Validates that the pipeline closes correctly - implements ICommandDelegate directly
    /// </summary>
    private sealed class PipelineChannelConsumerCommand : ICommandDelegate
    {
        private readonly Action? _onCompleted;

        public PipelineChannelConsumerCommand(Action? onCompleted)
        {
            _onCompleted = onCompleted;
        }

        public string Command => "PIPECHANNELCONSUMER";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            if (ioContext.HasPipedInput)
            {
                await foreach (var chunk in ioContext.ReadInputPipeChunks())
                {
                    yield return chunk;
                }

                _onCompleted?.Invoke();
            }
            else
            {
                yield return CommandResult<string>.Success(string.Empty);
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Pipeline_GracefullyHandlesParentCancellation()
    {
        var pipelineExecutor = new PipelineExecutor();
        var environmentContext = new ControllerEnvironmentContext(new EnvironmentContext());
        var rootContext = new TestTextIo();
        var commandStarted = new TaskCompletionSource<bool>();
        var cancellationRequested = new TaskCompletionSource<bool>();

        var commandFactories = new Dictionary<string, Func<ICommandDelegate>>(StringComparer.OrdinalIgnoreCase)
        {
            ["SLOWCOMMAND"] = () => new SlowCommand(commandStarted, cancellationRequested)
        };

        using var cts = new CancellationTokenSource();
        
        var runTask = pipelineExecutor.ExecuteAsync(
            "SLOWCOMMAND",
            rootContext,
            environmentContext,
            (name, ctx, env, ct) => ExecuteCommandWithCancellationAsync(name, ctx, env, ct, commandFactories),
            cts.Token);

        // Wait for command to start
        await commandStarted.Task;
        
        // Cancel the operation
        cts.Cancel();
        cancellationRequested.SetResult(true);

        // Verify that cancellation is propagated
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await runTask);
    }

    private static async Task ExecuteCommandWithCancellationAsync(
        string commandName,
        IIoContext ioContext,
        IEnvironmentContext environmentContext,
        CancellationToken cancellationToken,
        IDictionary<string, Func<ICommandDelegate>> commandFactories)
    {
        if (!commandFactories.TryGetValue(commandName, out var factory))
        {
            throw new InvalidOperationException($"Command [{commandName}] is not registered in the test harness.");
        }

        await using var command = factory();
        await foreach (var result in command.Main(ioContext, environmentContext).WithCancellation(cancellationToken))
        {
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Output))
            {
                await ioContext.OutputChunk(result).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Command that waits for cancellation - implements ICommandDelegate directly
    /// </summary>
    private sealed class SlowCommand : ICommandDelegate
    {
        private readonly TaskCompletionSource<bool> _commandStarted;
        private readonly TaskCompletionSource<bool> _cancellationRequested;

        public SlowCommand(TaskCompletionSource<bool> commandStarted, TaskCompletionSource<bool> cancellationRequested)
        {
            _commandStarted = commandStarted;
            _cancellationRequested = cancellationRequested;
        }

        public string Command => "SLOWCOMMAND";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            _commandStarted.SetResult(true);
            await _cancellationRequested.Task;
            yield return CommandResult<string>.Success("Should not reach here");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
