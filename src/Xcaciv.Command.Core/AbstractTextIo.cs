﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Core
{
    /// <summary>
    /// Implements the more generic parts of the ITextIoContext
    /// Allows for the implementation to handle the context specific parts
    /// </summary>
    /// <remarks>
    /// constructor requires a name and optional parent guid
    /// </remarks>
    /// <param name="name"></param>
    /// <param name="parentId"></param>
    public abstract class AbstractTextIo(string name, string[] parameters, Guid? parentId = default) : IIoContext
    {
        public bool Verbose { get; set; } = false;

        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; } = name;

        public Guid? Parent { get; protected set; } = parentId;

        public bool HasPipedInput { get; protected set; } = false;

        public string[] Parameters { get; set; } = parameters;

        public Task SetParameters(string[] parameters)
        {
            Parameters = parameters;
            return Task.CompletedTask;
        }

        protected ChannelReader<string>? inputPipe;
        protected ChannelWriter<string>? outputPipe;
        /// <summary>
        /// implementation must set the expected child's properties and pass environment values
        /// </summary>
        /// <param name="childArguments"></param>
        /// <returns></returns>
        public abstract Task<IIoContext> GetChild(string[]? childArguments = null);
        /// <summary>
        /// handles the Channel output and allows the implementation to handle 
        /// the final output
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual Task OutputChunk(string message)
        {
            if (outputPipe == null)
            {
                return HandleOutputChunk(message);
            }
            return outputPipe.WriteAsync(message).AsTask();
        }
        /// <summary>
        /// allow the implementation to handle the final output
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public abstract Task HandleOutputChunk(string chunk);
        /// <summary>
        /// allow the implementation to handle the prompting for input
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public abstract Task<string> PromptForCommand(string prompt);
        /// <summary>
        /// handles the Channel so the command just handles the await foreach
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ReadInputPipeChunks()
        {
            if (inputPipe == null) yield break;

            await foreach (var item in inputPipe.ReadAllAsync())
            {
                yield return item;
            }
        }
        /// <summary>
        /// set channel reader for pipeline
        /// </summary>
        /// <param name="reader"></param>
        public void SetInputPipe(ChannelReader<string> reader)
        {
            HasPipedInput = true;
            inputPipe = reader;
        }
        /// <summary>
        /// set channel writer for pipeline
        /// </summary>
        /// <param name="writer"></param>
        public void SetOutputPipe(ChannelWriter<string> writer)
        {
            outputPipe = writer;
        }
        /// <summary>
        /// display progress
        /// </summary>
        /// <param name="total"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public abstract Task<int> SetProgress(int total, int step);
        /// <summary>
        /// display status message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract Task SetStatusMessage(string message);
        /// <summary>
        /// complete the output pipe
        /// </summary>
        /// <returns></returns>
        public ValueTask DisposeAsync()
        {
            Complete().Wait();
            return ValueTask.CompletedTask;
        }

        public Task Complete(string? message = null)
        {
            if (!string.IsNullOrEmpty(message)) SetStatusMessage(message).Wait();

            outputPipe?.TryComplete();
            return Task.CompletedTask;
        }

        public void SetTraceLog(string logName)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(logName));
            Trace.AutoFlush = true;
            Trace.Indent();
            // TODO: listen to trace messages
            // if verbose send them to output
            // if not log to file 
        }

        public virtual Task AddTraceMessage(string message)
        {
            if (Verbose)
            {
                return OutputChunk("\tTRACE: " + message);
            }
            // if we are not verbose, send the output to DEBUG
            Trace.WriteLine(message);
            return Task.CompletedTask;
        }

    }
}
