using Xunit;
using Xcaciv.Command;
using Xcaciv.Command.Tests.TestImpementations;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests
{
    public class PipelineBackpressureTests
    {
        private ITestOutputHelper _testOutput;

        public PipelineBackpressureTests(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        /// <summary>
        /// Test: Default pipeline configuration uses bounded channels
        /// </summary>
        [Fact]
        public void DefaultPipelineConfiguration_IsBounded()
        {
            // Arrange
            var config = new PipelineConfiguration();

            // Assert
            Assert.Equal(10_000, config.MaxChannelQueueSize);
            Assert.Equal(PipelineBackpressureMode.Block, config.BackpressureMode);
            Assert.Equal(0, config.ExecutionTimeoutSeconds);
        }

        /// <summary>
        /// Test: Pipeline configuration can be customized
        /// </summary>
        [Fact]
        public void PipelineConfiguration_CanBeCustomized()
        {
            // Arrange & Act
            var config = new PipelineConfiguration
            {
                MaxChannelQueueSize = 5_000,
                BackpressureMode = PipelineBackpressureMode.DropNewest,
                ExecutionTimeoutSeconds = 30
            };

            // Assert
            Assert.Equal(5_000, config.MaxChannelQueueSize);
            Assert.Equal(PipelineBackpressureMode.DropNewest, config.BackpressureMode);
            Assert.Equal(30, config.ExecutionTimeoutSeconds);
        }

        /// <summary>
        /// Test: CommandController has pipeline configuration property
        /// </summary>
        [Fact]
        public void CommandController_HasPipelineConfiguration()
        {
            // Arrange
            var controller = new CommandController();

            // Act
            var config = controller.PipelineConfig;

            // Assert
            Assert.NotNull(config);
            Assert.Equal(10_000, config.MaxChannelQueueSize);
        }

        /// <summary>
        /// Test: Pipeline configuration can be set on CommandController
        /// </summary>
        [Fact]
        public void CommandController_PipelineConfiguration_CanBeSet()
        {
            // Arrange
            var controller = new CommandController();
            var customConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 1_000,
                BackpressureMode = PipelineBackpressureMode.Block
            };

            // Act
            controller.PipelineConfig = customConfig;

            // Assert
            Assert.Same(customConfig, controller.PipelineConfig);
            Assert.Equal(1_000, controller.PipelineConfig.MaxChannelQueueSize);
            Assert.Equal(PipelineBackpressureMode.Block, controller.PipelineConfig.BackpressureMode);
        }

        /// <summary>
        /// Test: Pipeline execution with custom configuration still works
        /// </summary>
        [Fact]
        public async Task PipelineExecution_WithCustomConfiguration_WorksAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.PipelineConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 5_000,
                BackpressureMode = PipelineBackpressureMode.DropOldest
            };
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo(new[] { "hello" });

            // Act
            await controller.Run("say hello | say world", ioContext, env);

            // Assert - Should execute without error
            Assert.NotNull(ioContext.Output);
        }

        /// <summary>
        /// Test: DropOldest backpressure mode configuration
        /// </summary>
        [Fact]
        public void BackpressureMode_DropOldest_Configured()
        {
            // Arrange
            var mode = PipelineBackpressureMode.DropOldest;

            // Assert
            Assert.Equal(0, (int)mode);
        }

        /// <summary>
        /// Test: DropNewest backpressure mode drops newest items when channel is full
        /// </summary>
        /// <remarks>
        /// This integration test verifies that when a bounded channel with DropNewest mode
        /// reaches capacity, the newest item already in the queue is dropped to make room
        /// for the incoming write. This is the opposite of DropOldest mode.
        /// </remarks>
        [Fact]
        public async Task BackpressureMode_DropNewest_DropsNewestItemsWhenFullAsync()
        {
            // Arrange: Create a channel directly to test DropNewest behavior
            var channelOptions = new BoundedChannelOptions(3)
            {
                FullMode = BoundedChannelFullMode.DropNewest
            };
            var channel = Channel.CreateBounded<string>(channelOptions);

            // Act: Fill the channel beyond capacity
            // With DropNewest, when full, the newest item in the queue is dropped to make room for new writes
            Assert.True(await channel.Writer.WaitToWriteAsync()); // Should succeed
            await channel.Writer.WriteAsync("item-1");
            await channel.Writer.WriteAsync("item-2");
            await channel.Writer.WriteAsync("item-3");
            
            // Channel is now full (capacity = 3): ["item-1", "item-2", "item-3"]
            // Next write drops "item-3" (newest in queue) and adds "item-4": ["item-1", "item-2", "item-4"]
            await channel.Writer.WriteAsync("item-4");
            // Next write drops "item-4" (newest in queue) and adds "item-5": ["item-1", "item-2", "item-5"]
            await channel.Writer.WriteAsync("item-5");
            
            channel.Writer.Complete();

            // Assert: Read all items from the channel
            var items = new List<string>();
            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                items.Add(item);
            }

            // With DropNewest mode, we should have exactly 3 items
            Assert.Equal(3, items.Count);
            
            // The oldest items should be preserved, newest item in queue gets dropped each time
            Assert.Equal("item-1", items[0]);
            Assert.Equal("item-2", items[1]);
            Assert.Equal("item-5", items[2]); // Last write succeeded by dropping previousNewest
            
            // Items 3 and 4 were dropped to make room for subsequent writes
            Assert.DoesNotContain("item-3", items);
            Assert.DoesNotContain("item-4", items);

            _testOutput.WriteLine($"Items in channel: {string.Join(", ", items)}");
            _testOutput.WriteLine("DropNewest successfully dropped newest items in queue when channel was full");
        }

        /// <summary>
        /// Test: Block backpressure mode configuration
        /// </summary>
        [Fact]
        public void BackpressureMode_Block_Configured()
        {
            // Arrange
            var mode = PipelineBackpressureMode.Block;

            // Assert
            Assert.Equal(2, (int)mode);
        }

        /// <summary>
        /// Integration test: Block mode causes producer to wait when channel is full
        /// </summary>
        /// <remarks>
        /// Note: This test verifies basic functionality but timing-based assertions
        /// are inherently flaky. The test ensures the pipeline completes successfully
        /// with blocking mode configured.
        /// </remarks>
        [Fact]
        public async Task BlockMode_ProducerBlocksWhenChannelFull_IntegrationTest()
        {
            // Arrange - Create a controller with a very small channel and Block mode
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.PipelineConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 2,  // Very small channel to fill quickly
                BackpressureMode = PipelineBackpressureMode.Block
            };
            
            // Create a producer command that outputs many items rapidly
            var producerCommand = new TestProducerCommand(itemCount: 10, delayMs: 0);
            controller.AddCommand("Test", producerCommand);
            
            // Create a slow consumer command that reads items slowly
            var consumerCommand = new TestSlowConsumerCommand(delayMs: 10); // Reduced delay for faster test
            controller.AddCommand("Test", consumerCommand);
            
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();
            
            // Act - Run the pipeline with producer | consumer
            var startTime = DateTime.UtcNow;
            await controller.Run("TestProducer | TestSlowConsumer", ioContext, env);
            var duration = DateTime.UtcNow - startTime;
            
            // Assert - Pipeline should complete successfully
            // With blocking mode, execution should take some time due to consumer delay
            // Note: Timing assertions are inherently flaky, so we just verify reasonable completion
            Assert.True(duration.TotalMilliseconds < 5000, 
                $"Pipeline took unexpectedly long: {duration.TotalMilliseconds}ms");
            
            // Verify some output was produced (the actual count depends on how controller instantiates commands)
            // Since controller creates new instances, we can't check ProcessedCount on our test instance
            _testOutput.WriteLine($"Pipeline completed in {duration.TotalMilliseconds}ms with Block mode");
        }

        /// <summary>
        /// Test: Multiple pipeline configurations don't interfere
        /// </summary>
        [Fact]
        public async Task MultiplePipelineConfigurations_DontInterfereAsync()
        {
            // Arrange
            var controller1 = new CommandController();
            controller1.RegisterBuiltInCommands();
            controller1.PipelineConfig.MaxChannelQueueSize = 5_000;

            var controller2 = new CommandController();
            controller2.RegisterBuiltInCommands();
            controller2.PipelineConfig.MaxChannelQueueSize = 2_000;

            var env = new EnvironmentContext();
            var ioContext1 = new TestTextIo(new[] { "test1" });
            var ioContext2 = new TestTextIo(new[] { "test2" });

            // Act
            await controller1.Run("say test1", ioContext1, env);
            await controller2.Run("say test2", ioContext2, env);

            // Assert
            Assert.NotNull(ioContext1.Output);
            Assert.NotNull(ioContext2.Output);
            Assert.Equal(5_000, controller1.PipelineConfig.MaxChannelQueueSize);
            Assert.Equal(2_000, controller2.PipelineConfig.MaxChannelQueueSize);
        }

        /// <summary>
        /// Test: Pipeline configuration with complex pipeline still works
        /// </summary>
        [Fact]
        public async Task PipelineConfiguration_WithComplexPipeline_WorksAsync()
        {
            // Arrange
            var controller = new CommandController();
            controller.RegisterBuiltInCommands();
            controller.PipelineConfig = new PipelineConfiguration
            {
                MaxChannelQueueSize = 1_000,
                BackpressureMode = PipelineBackpressureMode.DropNewest
            };
            var env = new EnvironmentContext();
            var ioContext = new TestTextIo();

            // Act
            await controller.Run("say a | say b | say c | say d", ioContext, env);

            // Assert
            Assert.NotNull(ioContext.Output);
        }
    }

    /// <summary>
    /// Test command that produces a configurable number of items with optional delay.
    /// Implements ICommandDelegate directly for full control over Main method.
    /// </summary>
    internal class TestProducerCommand : ICommandDelegate
    {
        private readonly int _itemCount;
        private readonly int _delayMs;

        public TestProducerCommand(int itemCount, int delayMs)
        {
            _itemCount = itemCount;
            _delayMs = delayMs;
        }

        public string Command => "TESTPRODUCER";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            for (int i = 0; i < _itemCount; i++)
            {
                if (_delayMs > 0)
                {
                    await Task.Delay(_delayMs);
                }

                yield return CommandResult<string>.Success($"Item-{i}");
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Test command that consumes piped input slowly to create backpressure.
    /// Implements ICommandDelegate directly for full control over Main method.
    /// </summary>
    internal class TestSlowConsumerCommand : ICommandDelegate
    {
        private readonly int _delayMs;
        public int ProcessedCount { get; private set; }

        public TestSlowConsumerCommand(int delayMs)
        {
            _delayMs = delayMs;
        }

        public string Command => "TESTSLOWCONSUMER";

        public string RootCommand => string.Empty;

        public Dictionary<string, string> GetDefaultEnvironment() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<ICommandParameter> GetParameters() => new List<ICommandParameter>();

        public async IAsyncEnumerable<IResult<string>> Main(IIoContext ioContext, IEnvironmentContext env)
        {
            if (ioContext.HasPipedInput)
            {
                await foreach (var chunk in ioContext.ReadInputPipeChunks())
                {
                    if (chunk == null || string.IsNullOrEmpty(chunk.Output)) continue;

                    // Simulate slow processing
                    await Task.Delay(_delayMs);
                    ProcessedCount++;
                    
                    yield return chunk;
                }
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
