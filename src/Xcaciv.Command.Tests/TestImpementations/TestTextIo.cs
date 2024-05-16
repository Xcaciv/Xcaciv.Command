﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command;
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

        public Dictionary<string, string> PromptAnswers { get; private set; } = new Dictionary<string, string>();

        public TestTextIo(string[]? arguments = null) : base("TestTextIo", null)
        {
            this.Parameters = arguments ?? string.Empty.Split(' ');
            this.Verbose = true;
        }

        public override Task<ITextIoContext> GetChild(string[]? childArguments = null)
        {
            var child = new TestTextIo(childArguments)
            {
                Parent = Id
            };
            Children.Add(child);
            return Task.FromResult<ITextIoContext>(child);
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

        public override Task HandleOutputChunk(string chunk)
        {
            Output.Add(chunk);
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            string output = string.Empty;
            if (HasPipedInput)
            {
                // combine output into one string seperated by new lines
                // and then add the children output
                output = string.Join(Environment.NewLine, Output);
                foreach (var chidl in Children)
                {
                    output += chidl.ToString() + Environment.NewLine;
                }
            }



            output += string.Join('-', Output);

            return output;
        }

    }
}
