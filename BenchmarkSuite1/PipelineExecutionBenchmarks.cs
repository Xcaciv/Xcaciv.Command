using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Benchmarks
{
    /// <summary>
    /// Benchmarks for PipelineExecutor to measure throughput and backpressure handling.
    /// Establishes baseline for threading optimization.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [SimpleJob(RuntimeMoniker.HostProcess)]
    public class PipelineExecutionBenchmarks
    {
        private CommandController _controller = null!;
        private TestTextIo _ioContext = null!;
        private ControllerEnvironmentContext _envContext = null!;

        [GlobalSetup]
        public void Setup()
        {
            _controller = new CommandController();
            _controller.RegisterBuiltInCommands();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _ioContext = new TestTextIo("benchmark", Array.Empty<string>());
            _envContext = new ControllerEnvironmentContext();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            if (_ioContext != null)
            {
                _ioContext.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Baseline: Single command execution (no pipeline)
        /// </summary>
        [Benchmark(Baseline = true)]
        public void SingleCommand()
        {
            _controller.Run("SAY Hello", _ioContext, _envContext).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Diagnostic: Verify command execution completes
        /// </summary>
        [Benchmark]
        public int SingleCommandWithValidation()
        {
            _controller.Run("SAY Hello", _ioContext, _envContext).GetAwaiter().GetResult();
            return _ioContext.Outputs.Count;
        }

        /// <summary>
        /// 2-stage pipeline throughput
        /// </summary>
        [Benchmark]
        public void TwoStagePipeline()
        {
            _controller.Run("SAY Hello | SAY World", _ioContext, _envContext).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 3-stage pipeline throughput
        /// </summary>
        [Benchmark]
        public void ThreeStagePipeline()
        {
            _controller.Run("SAY Hello | SAY World | SAY End", _ioContext, _envContext).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 5-stage pipeline - stress test
        /// </summary>
        [Benchmark]
        public void FiveStagePipeline()
        {
            _controller.Run("SAY One | SAY Two | SAY Three | SAY Four | SAY Five", _ioContext, _envContext).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Pipeline with backpressure (Block mode, default)
        /// </summary>
        [Benchmark]
        public void PipelineWithBackpressure_Block()
        {
            _controller.PipelineConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 10,
                BackpressureMode = PipelineBackpressureMode.Block
            };
            _controller.Run("SAY Test1 | SAY Test2 | SAY Test3", _ioContext, _envContext).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Pipeline with DropOldest backpressure
        /// </summary>
        [Benchmark]
        public void PipelineWithBackpressure_DropOldest()
        {
            _controller.PipelineConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 10,
                BackpressureMode = PipelineBackpressureMode.DropOldest
            };
            _controller.Run("SAY Test1 | SAY Test2 | SAY Test3", _ioContext, _envContext).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Pipeline with DropNewest backpressure
        /// </summary>
        [Benchmark]
        public void PipelineWithBackpressure_DropNewest()
        {
            _controller.PipelineConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 10,
                BackpressureMode = PipelineBackpressureMode.DropNewest
            };
            _controller.Run("SAY Test1 | SAY Test2 | SAY Test3", _ioContext, _envContext).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Minimal test IO context for benchmarking
    /// </summary>
    internal class TestTextIo(string name, string[] parameters, Guid? parentId = null) : AbstractTextIo(name, parameters, parentId)
    {
        private readonly List<IResult<string>> _outputs = new();

        public IReadOnlyList<IResult<string>> Outputs => _outputs;

        public override Task<IIoContext> GetChild()
        {
            return Task.FromResult<IIoContext>(new TestTextIo($"{Name}_child", Parameters, Id));
        }

        public override Task HandleOutputChunk(IResult<string> result)
        {
            _outputs.Add(result);
            return Task.CompletedTask;
        }

        public override Task<string> PromptForCommand(string prompt)
        {
            return Task.FromResult(string.Empty);
        }

        public override Task<int> SetProgress(int total, int step)
        {
            return Task.FromResult(0);
        }

        public override Task SetStatusMessage(string message)
        {
            return Task.CompletedTask;
        }
    }
}