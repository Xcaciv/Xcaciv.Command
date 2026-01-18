using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command
{
    // Fix for CS8602: Ensure 'parameters' is not null before using the spread operator
    public class MemoryIoContext(string name = "MemoryIo", string[]? parameters = default, Guid parentId = default)
        : AbstractTextIo(name, parameters is not null ? [.. parameters] : [], parentId)
    {
        public ConcurrentBag<MemoryIoContext> Children { get; private set; } = new ConcurrentBag<MemoryIoContext>();
        public ConcurrentBag<string> Output { get; private set; } = new ConcurrentBag<string>();
        public ConcurrentDictionary<string, string> PromptAnswers { get; private set; } = new ConcurrentDictionary<string, string>();

        public override Task<IIoContext> GetChild()
        {
            var child = new MemoryIoContext(this.Name + "Child", Parameters, Id);

            Children.Add(child);

            if (this.HasPipedInput && this.inputPipe != null) child.SetInputPipe(this.inputPipe);

            if (this.outputPipe != null) child.SetOutputPipe(this.outputPipe);

            return Task.FromResult<IIoContext>(child);
        }

        public override Task HandleOutputChunk(IResult<string> result)
        {
            if (result.IsSuccess && result.Output != null)
            {
                Output.Add(result.Output);
            }
            else if (!result.IsSuccess)
            {
                Output.Add($"ERROR: {result.ErrorMessage}");
            }
            return Task.CompletedTask;
        }

        public override Task<string> PromptForCommand(string prompt)
        {
            Output.Add($"PROMPT> {prompt}:");

            var answer = PromptAnswers.ContainsKey(prompt) ?
                    PromptAnswers[prompt] :
                    prompt;

            return Task.FromResult(answer);
        }

        public override Task<int> SetProgress(int total, int step)
        {
            Output.Add($"Progress: {step} of {total}");
            return Task.FromResult(step);
        }

        public override Task SetStatusMessage(string message)
        {
            Output.Add($"Status: {message}");
            return Task.CompletedTask;
        }
    }
}
