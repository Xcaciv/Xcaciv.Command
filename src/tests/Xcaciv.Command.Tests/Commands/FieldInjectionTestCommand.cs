using System;
using System.Collections.Generic;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Tests.Commands
{
    /// <summary>
    /// Test command that properly inherits from AbstractCommand to test parameter field injection.
    /// Uses actual attributes and the base ProcessParameters implementation.
    /// </summary>
    [CommandRegister("FieldTest", "Test command for field injection")]
    [CommandParameterOrdered("FirstParam", "First ordered parameter", IsRequired = false)]
    [CommandParameterOrdered("SecondParam", "Second ordered parameter", IsRequired = false)]
    [CommandParameterNamed("NamedParam", "A named parameter", IsRequired = false)]
    [CommandFlag("FlagParam", "A boolean flag")]
    [CommandParameterSuffix("SuffixParam", "Suffix parameter", IsRequired = false)]
    public class FieldInjectionTestCommand : AbstractCommand
    {
        // Public fields that should be auto-populated from parameters
        public string? FirstParam;
        public string? SecondParam;
        public string? NamedParam;
        public bool FlagParam;
        public string? SuffixParam;

        // Track if HandleExecution was called
        public bool ExecutionCalled { get; private set; }
        public Dictionary<string, IParameterValue>? ReceivedParameters { get; private set; }

        // Track if HandlePipedChunk was called
        public bool PipedChunkCalled { get; private set; }
        public IResult<string>? ReceivedPipedChunk { get; private set; }

        public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            ExecutionCalled = true;
            ReceivedParameters = parameters;

            // Build output showing both dictionary and field values
            var output = $"FirstParam: field={FirstParam ?? "null"}, dict={GetParamValue(parameters, "FirstParam")}\n" +
                         $"SecondParam: field={SecondParam ?? "null"}, dict={GetParamValue(parameters, "SecondParam")}\n" +
                         $"NamedParam: field={NamedParam ?? "null"}, dict={GetParamValue(parameters, "NamedParam")}\n" +
                         $"FlagParam: field={FlagParam}, dict={GetParamValue(parameters, "FlagParam")}\n" +
                         $"SuffixParam: field={SuffixParam ?? "null"}, dict={GetParamValue(parameters, "SuffixParam")}";
            Console.WriteLine(output);
            return CommandResult<string>.Success(output, this.OutputFormat);
        }

        public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            PipedChunkCalled = true;
            ReceivedPipedChunk = pipedChunk;
            ReceivedParameters = parameters;

            // Verify we can access the piped result properly
            var input = pipedChunk.Output ?? string.Empty;
            var isSuccess = pipedChunk.IsSuccess;

            var output = $"Piped: {input}, Success: {isSuccess}, FirstParam: {FirstParam ?? "null"}";
            return CommandResult<string>.Success(output, this.OutputFormat);
        }

        private string GetParamValue(Dictionary<string, IParameterValue> parameters, string key)
        {
            if (parameters.TryGetValue(key, out var param) && param.IsValid)
            {
                return param.RawValue?.ToString() ?? "null";
            }
            return "missing";
        }

        // Helper method to verify fields were set correctly
        public bool VerifyFieldsMatch(Dictionary<string, IParameterValue> parameters)
        {
            var firstMatch = FirstParam == GetParamValue(parameters, "FirstParam") || 
                           (FirstParam == null && GetParamValue(parameters, "FirstParam") == "missing");
            var secondMatch = SecondParam == GetParamValue(parameters, "SecondParam") || 
                            (SecondParam == null && GetParamValue(parameters, "SecondParam") == "missing");
            var namedMatch = NamedParam == GetParamValue(parameters, "NamedParam") || 
                           (NamedParam == null && GetParamValue(parameters, "NamedParam") == "missing");
            var suffixMatch = SuffixParam == GetParamValue(parameters, "SuffixParam") || 
                            (SuffixParam == null && GetParamValue(parameters, "SuffixParam") == "missing");
            
            var flagValue = parameters.TryGetValue("FlagParam", out var flag) && flag.IsValid 
                ? flag.GetValue<bool>() 
                : false;
            var flagMatch = FlagParam == flagValue;

            return firstMatch && secondMatch && namedMatch && flagMatch && suffixMatch;
        }
    }
}
