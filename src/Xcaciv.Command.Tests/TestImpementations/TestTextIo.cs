using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Tests.TestImpementations
{
    public class TestTextIo : AbstractTextIo
    {
        /// <summary>
        /// test collection of children to verify commmand behavior
        /// </summary>
        public List<TestTextIo> Children { get; private set; } = new List<TestTextIo>();

        /// <summary>
        /// test collection of output chunks to verify behavior
        /// </summary>
        public List<string> Output { get; private set; } = new List<string>();

        public List<string> Trace { get; private set; } = new List<string>();

        public Dictionary<string, string> PromptAnswers { get; private set; } = new Dictionary<string, string>();

        public TestTextIo(string[]? parameters = null) : base("TestTextIo", parameters ?? [], default)
        {
            this.Verbose = true;
        }

        public override Task<IIoContext> GetChild()
        {
            var child = new TestTextIo(this.Parameters)
            {
                Parent = Id
            };

            Children.Add(child);

            if (this.HasPipedInput && this.inputPipe != null) child.SetInputPipe(this.inputPipe);
            
            if (this.outputPipe != null) child.SetOutputPipe(this.outputPipe);
            
            return Task.FromResult<IIoContext>(child);
        }

        public override Task<string> PromptForCommand(string prompt)
        {
            Output.Add($"PROMPT> {prompt}");

            var answer = PromptAnswers.ContainsKey(prompt) ?
                    PromptAnswers[prompt] :
                    prompt;

            return Task.FromResult(answer);
        }

        public override Task<int> SetProgress(int total, int step)
        {
            return Task.FromResult(step);
        }

        public override Task SetStatusMessage(string message)
        {
            Output.Add(message);
            return Task.CompletedTask;
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

        public override Task AddTraceMessage(string message)
        {
            Trace.Add(message);
            System.Diagnostics.Debug.WriteLine(message);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            string output = string.Empty;
            
            // Gather child output first if there are children
            if (Children.Count > 0)
            {
                output = GatherChildOutput();
            }

            // Add this context's output
            var localOutput = string.Join(Environment.NewLine, Output);
            if (!string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(localOutput))
            {
                // Both have content - join with newline
                output += Environment.NewLine + localOutput;
            }
            else if (!string.IsNullOrEmpty(localOutput))
            {
                // Only local output
                output = localOutput;
            }
            // else: only child output (already in 'output') or nothing

            return output;
        }

        private string GatherChildOutput()
        {
            // combine output into one string separated by new lines
            // and then add the children output
            var childOutputs = new List<string>();
            foreach (var child in Children)
            {
                var childOutput = child.ToString();
                if (!string.IsNullOrEmpty(childOutput))
                {
                    childOutputs.Add(childOutput);
                }
            }

            return string.Join(Environment.NewLine, childOutputs);
        }
    }
}
