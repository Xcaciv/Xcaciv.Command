# Command Implementation Template

This document provides a template and guidelines for implementing new commands in the Xcaciv.Command framework.

## Quick Reference: Parameter Attributes

| Attribute | Usage | Notes |
|-----------|-------|-------|
| `CommandRegisterAttribute` | **Required** on class | Registers the command with a name and description |
| `CommandParameterOrderedAttribute` | Position-based parameters | Must precede named parameters |
| `CommandParameterNamedAttribute` | Named parameters (with `-name` flag) | Used with `-name value` syntax |
| `CommandFlagAttribute` | Boolean toggle flags | Presence = true, absence = false |
| `CommandParameterSuffixAttribute` | Capture remaining arguments | Collects all remaining args as single value |
| `CommandHelpRemarksAttribute` | Additional help information | Multiple remarks can be added |
| `CommandRootAttribute` | Multi-level sub-commands | For commands with sub-commands |

## Interface Alignment

- `AbstractCommand` implements `ICommandDelegate` (which includes `Main`, `Help`, and `OneLineHelp`) and `IAsyncDisposable`; most commands only override `HandleExecution` and `HandlePipedChunk`.
- Use `OutputFormat` to declare the serialization shape of your output (defaults to `ResultFormat.General`).
- Override `DisposeAsync` when your command owns disposable resources. Add `using System.Threading.Tasks;` and `using Xcaciv.Command.Interface;` when you override it.

```csharp
public MyCommand()
{
    OutputFormat = ResultFormat.General; // Or ResultFormat.JSON/CSV/TDL/YAML
}

public override ValueTask DisposeAsync()
{
    // Release disposable resources here.
    return base.DisposeAsync();
}
```

## Important: HandlePipedChunk Signature (v3.2.3+)

As of version 3.2.3, `HandlePipedChunk` accepts `IResult<string>` instead of `string`:

```csharp
// New signature (v3.2.3+)
public override IResult<string> HandlePipedChunk(
    IResult<string> pipedChunk, 
    Dictionary<string, IParameterValue> parameters, 
    IEnvironmentContext env)
{
    // Access the output string
    var input = pipedChunk.Output ?? string.Empty;
    
    // Optionally check if upstream command succeeded
    if (!pipedChunk.IsSuccess)
    {
        // Handle or propagate error
        return pipedChunk;
    }
    
    // Process the input
    var result = ProcessInput(input);
    return CommandResult<string>.Success(result, this.OutputFormat);
}
```

This allows commands to:
- Check `pipedChunk.IsSuccess` for upstream failures
- Access `pipedChunk.ErrorMessage` and `pipedChunk.Exception`
- Retrieve `pipedChunk.ResultFormat` and `pipedChunk.CorrelationId`

## Basic Command Template

```csharp
using System;
using System.Collections.Generic;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Xcaciv.Command.Commands
{
    [CommandRegister("MyCommand", "Description of what the command does", Prototype = "MYCOMMAND <param1> <param2>")]
    [CommandParameterOrdered("param1", "Description of first parameter")]
    [CommandParameterOrdered("param2", "Description of second parameter", DataType = typeof(int))]
    [CommandParameterNamed("param3", "Description of named parameter", IsRequired = false, AllowedValues = ["value1", "value2"])]
    internal class MyCommand : AbstractCommand
    {
        public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            // Extract typed parameters safely
            var param1 = parameters.TryGetValue("param1", out var p1) && p1.IsValid 
                ? p1.GetValue<string>() 
                : string.Empty;
            
            var param2 = parameters.TryGetValue("param2", out var p2) && p2.IsValid 
                ? p2.GetValue<string>() 
                : string.Empty;

            var param3 = parameters.TryGetValue("param3", out var p3) && p3.IsValid 
                ? p3.GetValue<string>() 
                : "default";

            // Execute command logic
            var result = $"Processed: {param1}, {param2}, {param3}";
            
            return CommandResult<string>.Success(result, this.OutputFormat);
        }

        public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
        {
            // Handle piped input - extract the output string
            var input = pipedChunk.Output ?? string.Empty;
            return CommandResult<string>.Success(input.ToUpper(), this.OutputFormat);
        }
    }
}
```

## Pattern Examples

### 1. Simple Command (No Parameters)

```csharp
[CommandRegister("Now", "Display current timestamp", Prototype = "NOW")]
internal class NowCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        return CommandResult<string>.Success(DateTime.UtcNow.ToString("O"), this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        // Pass through piped input unchanged
        return CommandResult<string>.Success(pipedChunk.Output ?? string.Empty, this.OutputFormat);
    }
}
```

### 2. Command with Ordered Parameters

```csharp
[CommandRegister("Add", "Add two numbers", Prototype = "ADD <number1> <number2>")]
[CommandParameterOrdered("Number1", "First number")]
[CommandParameterOrdered("Number2", "Second number")]
internal class AddCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var num1 = parameters.TryGetValue("number1", out var p1) && p1.IsValid 
            ? p1.GetValue<int>() 
            : 0;
        var num2 = parameters.TryGetValue("number2", out var p2) && p2.IsValid 
            ? p2.GetValue<int>() 
            : 0;

        return CommandResult<string>.Success((num1 + num2).ToString(), this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        // Not designed for piping - return empty
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }
}
```

### 3. Command with Named Parameters

```csharp
[CommandRegister("Copy", "Copy with optional verbose flag", Prototype = "COPY <source> -dest <destination> [-v]")]
[CommandParameterOrdered("Source", "Source path")]
[CommandParameterNamed("Dest", "Destination path", IsRequired = true)]
[CommandFlag("Verbose", "Show verbose output")]
internal class CopyCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var source = parameters.TryGetValue("source", out var src) && src.IsValid 
            ? src.GetValue<string>() 
            : string.Empty;
        
        var dest = parameters.TryGetValue("dest", out var d) && d.IsValid 
            ? d.GetValue<string>() 
            : string.Empty;
        
        var verbose = parameters.TryGetValue("verbose", out var v) && v.IsValid 
            ? v.GetValue<bool>() 
            : false;

        // Execute copy logic
        var result = $"Copied {source} to {dest}" + (verbose ? " [verbose mode]" : "");
        return CommandResult<string>.Success(result, this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }
}
```

### 4. Command with Suffix Parameter (Capture All Remaining Args)

```csharp
[CommandRegister("Echo", "Echo text with interpolation", Prototype = "ECHO <text...>")]
[CommandParameterSuffix("text", "Text to echo")]
internal class EchoCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var text = parameters.TryGetValue("text", out var t) && t.IsValid 
            ? t.GetValue<string>() 
            : string.Empty;
        
        return CommandResult<string>.Success(text, this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        // Pass through piped input
        return CommandResult<string>.Success(pipedChunk.Output ?? string.Empty, this.OutputFormat);
    }
}
```

### 5. Command with Piped Input (SET pattern)

```csharp
[CommandRegister("Buffer", "Buffer piped input", Prototype = "BUFFER <name>")]
[CommandParameterOrdered("Name", "Variable name")]
[CommandParameterOrdered("Value", "Initial value", UsePipe = true)]
[CommandHelpRemarks("This command accumulates piped input into a variable.")]
internal class BufferCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var name = parameters.TryGetValue("name", out var n) && n.IsValid 
            ? n.GetValue<string>() 
            : string.Empty;
        var value = parameters.TryGetValue("value", out var v) && v.IsValid 
            ? v.GetValue<string>() 
            : string.Empty;

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
        {
            env.SetValue(name, value);
        }
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var name = parameters.TryGetValue("name", out var n) && n.IsValid 
            ? n.GetValue<string>() 
            : string.Empty;
        
        if (!string.IsNullOrEmpty(name))
        {
            var input = pipedChunk.Output ?? string.Empty;
            var current = env.GetValue(name);
            env.SetValue(name, current + input);
        }
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }

    protected override void OnStartPipe(Dictionary<string, IParameterValue> processedParameters, IEnvironmentContext environment)
    {
        var name = processedParameters.TryGetValue("name", out var n) && n.IsValid 
            ? n.GetValue<string>() 
            : string.Empty;
        
        if (!string.IsNullOrEmpty(name))
        {
            environment.SetValue(name, string.Empty);
        }
        base.OnStartPipe(processedParameters, environment);
    }
}
```

### 6. Command with Stateful Processing (REGIF pattern)

```csharp
[CommandRegister("Filter", "Filter piped input by condition", Prototype = "FILTER <condition>")]
[CommandParameterOrdered("Condition", "Filter condition")]
internal class FilterCommand : AbstractCommand
{
    private bool cachedCondition = false;

    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        // When not piping, just initialize
        if (parameters.TryGetValue("condition", out var c) && c.IsValid)
        {
            cachedCondition = c.GetValue<bool>();
        }
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var input = pipedChunk.Output ?? string.Empty;
        
        if (cachedCondition)
        {
            return CommandResult<string>.Success(input, this.OutputFormat);
        }
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }
}
```

### 7. Command with Error Propagation

```csharp
[CommandRegister("Validate", "Validate and process input", Prototype = "VALIDATE <rule>")]
[CommandParameterOrdered("Rule", "Validation rule")]
[CommandHelpRemarks("Checks if piped input matches the validation rule.")]
[CommandHelpRemarks("Propagates errors from upstream commands.")]
internal class ValidateCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        return CommandResult<string>.Success(string.Empty, this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        // Check if upstream command failed
        if (!pipedChunk.IsSuccess)
        {
            // Propagate the error to downstream commands
            return pipedChunk;
        }

        var input = pipedChunk.Output ?? string.Empty;
        var rule = parameters.TryGetValue("rule", out var r) && r.IsValid 
            ? r.GetValue<string>() 
            : string.Empty;

        // Validate the input
        if (IsValid(input, rule))
        {
            return CommandResult<string>.Success(input, this.OutputFormat);
        }
        else
        {
            return CommandResult<string>.Failure($"Validation failed: input does not match rule '{rule}'");
        }
    }

    private bool IsValid(string input, string rule)
    {
        // Implement validation logic
        return true;
    }
}
```

### 8. Command with Help Remarks and Aliases

```csharp
[CommandRegister("Count", "Count lines or characters", Prototype = "COUNT [-type lines|chars]")]
[CommandParameterNamed("Type", "Count type", DefaultValue = "lines", AllowedValues = ["lines", "chars"])]
[CommandHelpRemarks("Counts the number of lines or characters in piped input.")]
[CommandHelpRemarks("Default behavior counts lines. Use -type chars to count characters.")]
internal class CountCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        return CommandResult<string>.Success("0", this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var input = pipedChunk.Output ?? string.Empty;
        var type = parameters.TryGetValue("type", out var t) && t.IsValid 
            ? t.GetValue<string>() 
            : "lines";

        var count = type == "chars" 
            ? input.Length 
            : input.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None).Length;

        return CommandResult<string>.Success(count.ToString(), this.OutputFormat);
    }
}
```

## Best Practices

1. **Null Safety**: Always use `pipedChunk.Output ?? string.Empty` when accessing piped input
2. **Error Handling**: Check `pipedChunk.IsSuccess` if you need to handle upstream failures
3. **Error Propagation**: Return `pipedChunk` directly to propagate errors to downstream commands
4. **Parameter Validation**: Use `IsValid` property before calling `GetValue<T>()`
5. **Resource Cleanup**: Override `DisposeAsync` for commands that allocate resources
6. **Output Format**: Set `OutputFormat` in constructor if command produces structured data
7. **Help Documentation**: Use `CommandHelpRemarks` for additional usage information

## Migration from v3.2.2 to v3.2.3

If you have existing commands using the old signature:

```csharp
// Old (v3.2.2 and earlier)
public override IResult<string> HandlePipedChunk(
    string pipedChunk, 
    Dictionary<string, IParameterValue> parameters, 
    IEnvironmentContext env)
{
    return CommandResult<string>.Success(pipedChunk.ToUpper());
}

// New (v3.2.3+)
public override IResult<string> HandlePipedChunk(
    IResult<string> pipedChunk, 
    Dictionary<string, IParameterValue> parameters, 
    IEnvironmentContext env)
{
    var input = pipedChunk.Output ?? string.Empty;
    return CommandResult<string>.Success(input.ToUpper());
}
```

## End-to-End Workflow for a New Command Package

Use this workflow when you are creating a new Xcaciv.Command implementation in a standalone solution or repository.

### 1. Prefer `AbstractCommand` over a custom `ICommandDelegate`

The recommended implementation path is to inherit from `AbstractCommand` and decorate the class with attribute-driven parameter metadata. This keeps the command aligned with the framework’s normal registration, help generation, pipeline behavior, and environment semantics.

Only implement `ICommandDelegate` directly when you need a very custom execution model and you are prepared to support the command manually. For most teams, a custom `ICommandDelegate` is harder to maintain, harder to test, and less compatible with the command loader, help generation, and parameter system.

A good rule is:

- Use `AbstractCommand` if the command should behave like a normal Xcaciv.Command plugin.
- Use `ICommandDelegate` only when you need a bespoke runtime contract or legacy compatibility.

### 2. Decide whether the command belongs in an existing project or a new package

If you already have a project that owns the command domain, add the command there.

If not, create a new class library project and a solution for it.

Suggested structure:

```text
MyCommandPackage/
  MyCommandPackage.csproj
  Commands/
    MyTransformCommand.cs
  README.md
  tests/
    MyCommandPackage.Tests/
      MyCommandPackage.Tests.csproj
  artifacts/
    packages/
```

Before writing code, create a brief PRD if the command is new or cross-cutting. A simple PRD should include:

- Command name and purpose
- User-visible prototype, for example: `MYTRANSFORM <source> -dest <target> [-overwrite]`
- Ordered, named, flag, and suffix parameters
- Whether it reads or writes environment values
- Whether it accepts piped input and how it should behave
- Success and failure output shapes
- Error handling expectations and tracing
- Security and validation constraints
- Example invocation and expected output

### 3. Use a self-contained package project template

A standalone package should not depend on repository-specific file layouts. The project should be a normal class library, with package metadata and tests included in the same repo.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>Contoso.MyCommandPackage</PackageId>
    <Version>1.0.0</Version>
    <Authors>Contoso</Authors>
    <Company>Contoso</Company>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/contoso/MyCommandPackage</PackageProjectUrl>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryUrl>https://github.com/contoso/MyCommandPackage</RepositoryUrl>
    <PackageDescription>Custom Xcaciv.Command plugin package.</PackageDescription>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <PackageOutputPath>$(MSBuildThisFileDirectory)artifacts\packages</PackageOutputPath>
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)Key.snk</AssemblyOriginatorKeyFile>
    <PublicSign Condition="'$(OS)' != 'Windows_NT'">true</PublicSign>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Xcaciv.Command.Core\Xcaciv.Command.Core.csproj" PrivateAssets="all" />
    <ProjectReference Include="..\Xcaciv.Command.Interface\Xcaciv.Command.Interface.csproj" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

This is the minimum shape for a command package. The key point is that the package is self-contained: it can be copied into another repo, restored, built, tested, and packed without assumptions about the source tree layout.

### 4. Create the command implementation

The command should follow the standard plugin pattern:

```csharp
using System.Collections.Generic;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Parameters;

namespace Contoso.MyCommandPackage;

[CommandRegister("MYTRANSFORM", "Transforms text using a custom rule", Prototype = "MYTRANSFORM <input> -mode <fast|safe> [-verbose]")]
[CommandParameterOrdered("Input", "Input value to transform")]
[CommandParameterNamed("Mode", "Processing mode", IsRequired = false, DefaultValue = "safe", AllowedValues = ["fast", "safe"])]
[CommandFlag("Verbose", "Emit additional output")]
public class MyTransformCommand : AbstractCommand
{
    public override IResult<string> HandleExecution(Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        var input = parameters.TryGetValue("input", out var inputParam) && inputParam.IsValid
            ? inputParam.GetValue<string>()
            : string.Empty;

        var mode = parameters.TryGetValue("mode", out var modeParam) && modeParam.IsValid
            ? modeParam.GetValue<string>()
            : "safe";

        var verbose = parameters.TryGetValue("verbose", out var verboseParam) && verboseParam.IsValid
            ? verboseParam.GetValue<bool>()
            : false;

        var result = mode == "fast"
            ? input.Trim().ToUpperInvariant()
            : input.Trim();

        if (verbose)
        {
            result += " [verbose]";
        }

        return CommandResult<string>.Success(result, this.OutputFormat);
    }

    public override IResult<string> HandlePipedChunk(IResult<string> pipedChunk, Dictionary<string, IParameterValue> parameters, IEnvironmentContext env)
    {
        if (!pipedChunk.IsSuccess)
        {
            return pipedChunk;
        }

        var input = pipedChunk.Output ?? string.Empty;
        return CommandResult<string>.Success(input.Trim(), this.OutputFormat);
    }
}
```

This preserves the framework’s recommended structure:

- `CommandRegisterAttribute` gives the command its public name and help metadata.
- Parameter attributes define ordering, names, and validation.
- `HandleExecution` does the main work.
- `HandlePipedChunk` manages streamed input safely and propagates upstream failures.

### 5. Add test configuration for the package

You should fully test the package before creating a signed NuGet package. A practical setup is:

1. Create a class library for the command plugin.
2. Create a separate xUnit test project that loads the plugin directory.
3. Use `CommandController`, `MemoryIoContext`, and the package directory loading workflow.

Example test project:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyCommandPackage\MyCommandPackage.csproj" />
    <ProjectReference Include="..\Xcaciv.Command\Xcaciv.Command.csproj" />
  </ItemGroup>

</Project>
```

Example test code:

```csharp
using Xunit;
using Xcaciv.Command;

public class MyTransformCommandTests
{
    [Fact]
    public async Task MyTransformCommand_ExecutesSuccessfully()
    {
        var controller = new CommandController();
        controller.AddPackageDirectory("C:/path/to/package/bin/Debug/net10.0");
        controller.LoadCommands();

        var io = new MemoryIoContext();
        var env = new ControllerEnvironmentContext();

        var result = await controller.Run("MYTRANSFORM hello -mode safe", io, env);

        Assert.NotNull(result);
        Assert.Contains("hello", result.ToString());
    }
}
```

For a full validation pass, check these scenarios:

- Help output for `--HELP`
- Required parameter validation
- Optional parameter behavior
- Flag parsing for switches like `-verbose`
- Piped input processing
- Failure propagation from upstream commands
- Environment mutation, if the command writes values
- Package discovery through `AddPackageDirectory()` and `LoadCommands()`

### 6. Load the package into the controller for end-to-end validation

The command package must be discoverable by the framework at runtime. Use the standard plugin loading pattern:

```csharp
var controller = new CommandController();
controller.AddPackageDirectory("C:/path/to/package/bin/Debug/net10.0");
controller.LoadCommands();

var io = new MemoryIoContext();
var env = new ControllerEnvironmentContext();

await controller.Run("MYTRANSFORM hello -mode safe -verbose", io, env);
```

If the command is in an existing project, you can register it directly with the controller instead of using a plugin directory.

### 7. Create a signed NuGet package

When the command is ready, package it as a signed NuGet artifact. This usually means both assembly signing and package signing.

#### Assembly signing

Add strong-name signing to the project:

```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)Key.snk</AssemblyOriginatorKeyFile>
  <PublicSign Condition="'$(OS)' != 'Windows_NT'">true</PublicSign>
</PropertyGroup>
```

Generate a key file once:

```powershell
sn -k Key.snk
```

#### Package signing

Package signing is typically done with a certificate that matches your organization’s signing policy. If you have a certificate thumbprint:

```powershell
dotnet pack MyCommandPackage.csproj -c Release -p:PackageOutputPath=artifacts\packages -p:SignPackage=true -p:CertificateThumbprint="<thumbprint>"
```

If you are using NuGet’s signing workflow instead of MSBuild property-based signing, use:

```powershell
nuget sign artifacts\packages\Contoso.MyCommandPackage.1.0.0.nupkg -CertificateSubjectName "Contoso" -Timestamper "http://timestamp.digicert.com"
```

For a repository that already follows the framework’s packaging conventions, set the core package properties explicitly:

- `GeneratePackageOnBuild` set to true
- `IncludeSymbols` enabled
- `SymbolPackageFormat` set to `snupkg`
- `PackageReadmeFile` provided
- `PackageLicenseExpression` set explicitly
- `PackageOutputPath` configured for artifact output

### 8. Recommended validation checklist before release

Before publishing the package, validate all of the following:

- The command class loads without reflection errors.
- Help text is generated correctly.
- Required parameters fail gracefully when missing.
- Named flags parse as expected.
- Pipelines continue to propagate success and failure states.
- The package loads from a directory using `AddPackageDirectory()`.
- The working package can be packed in Release mode.
- The signed package is accepted by your distribution process or internal feed.

### 9. Practical recommendation

If the command is new and not deeply coupled to a legacy implementation, create the PRD first, then implement the command in the smallest relevant class library, then validate it with an isolated xUnit project, and finally pack and sign the package. This keeps the command aligned with the rest of Xcaciv.Command and reduces long-term maintenance cost.

If you are moving this template into its own repository, keep it self-contained: include the repository README, project scaffolding, signing guidance, and test samples, but avoid references to local tree layouts or sibling repositories.

---

## Quick checklist for a new command implementation

- [ ] Decide if `AbstractCommand` is the right base class.
- [ ] Brainstorm the PRD before coding.
- [ ] Create or choose a class library project.
- [ ] Add references to the Xcaciv.Command packages.
- [ ] Implement the command with `CommandRegisterAttribute` and parameter attributes.
- [ ] Handle piped input and upstream failure propagation correctly.
- [ ] Add xUnit tests for happy-path and failure-path execution.
- [ ] Validate runtime loading from a package directory.
- [ ] Pack the package in Release mode.
- [ ] Sign the assembly and the NuGet package.
- [ ] Publish to your package source or internal feed.

This gives you a consistent path from design to deployment while keeping the command aligned with the Xcaciv.Command framework’s expected behavior and package conventions.

