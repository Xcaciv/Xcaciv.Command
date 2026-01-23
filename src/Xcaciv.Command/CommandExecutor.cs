using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;

namespace Xcaciv.Command;

/// <summary>
/// Executes commands and handles help routing, audit logging, and environment updates.
/// </summary>
public class CommandExecutor : ICommandExecutor
{
    private readonly ICommandRegistry _registry;
    private readonly ICommandFactory _commandFactory;
    private readonly IHelpService _helpService;
    private IAuditLogger _auditLogger = new NoOpAuditLogger();

    public CommandExecutor(ICommandRegistry registry, ICommandFactory commandFactory, IHelpService helpService)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
        _helpService = helpService ?? throw new ArgumentNullException(nameof(helpService));
    }

    public string HelpCommand { get; set; } = "HELP";

    public IAuditLogger AuditLogger
    {
        get => _auditLogger;
        set => _auditLogger = value ?? new NoOpAuditLogger();
    }

    public async Task ExecuteAsync(string commandKey, IIoContext ioContext, IEnvironmentContext environmentContext)
    {
        await ExecuteAsync(commandKey, ioContext, environmentContext, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(string commandKey, IIoContext ioContext, IEnvironmentContext environmentContext, CancellationToken cancellationToken)
    {
        if (commandKey == null) throw new ArgumentNullException(nameof(commandKey));
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));
        if (environmentContext == null) throw new ArgumentNullException(nameof(environmentContext));

        cancellationToken.ThrowIfCancellationRequested();

        if (_registry.TryGetCommand(commandKey, out var commandDescription))
        {
            if (commandDescription == null)
            {
                await ioContext.AddTraceMessage($"Command registry returned null description for key: {commandKey}").ConfigureAwait(false);
                await ioContext.OutputChunk(CommandResult<string>.Failure($"Command [{commandKey}] not found.")).ConfigureAwait(false);
                return;
            }

            await ExecuteCommandWithErrorHandling(commandDescription, ioContext, environmentContext, commandKey).ConfigureAwait(false);
            return;
        }

        var message = $"Command [{commandKey}] not found.";
        await ioContext.OutputChunk(CommandResult<string>.Failure($"{message} Try '{HelpCommand}'")).ConfigureAwait(false);
        await ioContext.AddTraceMessage(message).ConfigureAwait(false);
    }

    public Task GetHelpAsync(string command, IIoContext ioContext, IEnvironmentContext environmentContext)
    {
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));
        if (environmentContext == null) throw new ArgumentNullException(nameof(environmentContext));

        return string.IsNullOrEmpty(command)
            ? OutputAllCommands(ioContext)
            : OutputCommandHelp(command, ioContext, environmentContext);
    }

    public Task GetHelpAsync(string command, IIoContext ioContext, IEnvironmentContext environmentContext, CancellationToken cancellationToken)
    {
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));
        if (environmentContext == null) throw new ArgumentNullException(nameof(environmentContext));

        // Help generation is synchronous and doesn't perform cancellable operations
        // Accept token for API consistency but delegate to non-cancellable version
        return GetHelpAsync(command, ioContext, environmentContext);
    }

    private async Task OutputAllCommands(IIoContext context)
    {
        foreach (var description in _registry.GetAllCommands())
        {
            if (description.SubCommands.Count > 0 && string.IsNullOrEmpty(description.FullTypeName))
            {
                var firstSubCommand = description.SubCommands.First().Value;
                var commandInstance = _commandFactory.CreateCommand(firstSubCommand, context);
                var commandType = commandInstance.GetType();

                if (Attribute.GetCustomAttribute(commandType, typeof(CommandRootAttribute)) is CommandRootAttribute rootAttribute)
                {
                    await context.OutputChunk(CommandResult<string>.Success($"{description.BaseCommand,-12} {rootAttribute.Description}")).ConfigureAwait(false);
                }

                var subHelpLines = description.SubCommands.Select(subCommand => _helpService.BuildOneLineHelp(subCommand.Value));
                foreach (var subHelpLine in subHelpLines)
                {
                    await context.OutputChunk(CommandResult<string>.Success(subHelpLine)).ConfigureAwait(false);
                }
            }
            else
            {
                var helpLine = _helpService.BuildOneLineHelp(description);
                await context.OutputChunk(CommandResult<string>.Success(helpLine)).ConfigureAwait(false);
            }
        }
    }

    private async Task OutputCommandHelp(string command, IIoContext context, IEnvironmentContext env)
    {
        try
        {
            var commandKey = NamesValidator.GetValidCommandName(command);
            if (_registry.TryGetCommand(commandKey, out var description) && description != null)
            {
                // Check if this is a root command (has subcommands but no actual type)
                if (description.SubCommands.Count > 0 && string.IsNullOrEmpty(description.FullTypeName))
                {
                    // This is a root command - show the root description and all subcommands
                    var firstSubCommand = description.SubCommands.First().Value;
                    var commandInstance = _commandFactory.CreateCommand(firstSubCommand, context);
                    var commandType = commandInstance.GetType();

                    if (Attribute.GetCustomAttribute(commandType, typeof(CommandRootAttribute)) is CommandRootAttribute rootAttribute)
                    {
                        await context.OutputChunk(CommandResult<string>.Success($"{description.BaseCommand}:")).ConfigureAwait(false);
                        await context.OutputChunk(CommandResult<string>.Success($"  {rootAttribute.Description}")).ConfigureAwait(false);
                        await context.OutputChunk(CommandResult<string>.Success(string.Empty)).ConfigureAwait(false);
                        await context.OutputChunk(CommandResult<string>.Success("Sub-commands:")).ConfigureAwait(false);
                    }

                    // Show all subcommands
                    foreach (var subCommand in description.SubCommands.Values)
                    {
                        var subHelpLine = _helpService.BuildOneLineHelp(subCommand);
                        await context.OutputChunk(CommandResult<string>.Success(subHelpLine)).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Regular command with a type - show detailed help
                    var commandInstance = _commandFactory.CreateCommand(description, context);
                    var helpText = _helpService.BuildHelp(commandInstance, context.Parameters, env);
                    await context.OutputChunk(CommandResult<string>.Success(helpText)).ConfigureAwait(false);
                }
            }
            else
            {
                await context.OutputChunk(CommandResult<string>.Failure($"Command [{commandKey}] not found.")).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await context.AddTraceMessage(
                $"Error getting help for command '{command}': {ex}").ConfigureAwait(false);
            var exceptionTypeName = ex.GetType().Name;
            await context.OutputChunk(CommandResult<string>.Failure(
                $"Error getting help for command '{command}' ({exceptionTypeName}: {ex.Message}). See trace for more details.", ex))
                .ConfigureAwait(false);
        }
    }

    private async Task ExecuteCommandWithErrorHandling(
        ICommandDescription commandDescription,
        IIoContext ioContext,
        IEnvironmentContext environmentContext,
        string commandKey)
    {
        var startTime = DateTime.UtcNow;
        var success = false;
        string? errorMessage = null;
        var resultFailures = new List<string>();

        try
        {
            await ioContext.AddTraceMessage($"ExecuteCommand: {commandKey} Start.").ConfigureAwait(false);

            var commandInstance = _commandFactory.CreateCommand(commandDescription, ioContext);

            await using (var childEnv = await environmentContext.GetChild().ConfigureAwait(false))
            {
                await foreach (var result in commandInstance.Main(ioContext, childEnv).ConfigureAwait(false))
                {
                    if (result == null)
                    {
                        continue;
                    }

                    if (result.IsSuccess)
                    {
                        if (!string.IsNullOrEmpty(result.Output))
                        {
                            await ioContext.OutputChunk(result).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        var failureMessage = result.ErrorMessage ?? $"Command [{commandKey}] reported failure (CorrelationId: {result.CorrelationId}).";
                        resultFailures.Add(failureMessage);
                        await ioContext.OutputChunk(CommandResult<string>.Failure(failureMessage, result.Exception)).ConfigureAwait(false);

                        if (result.Exception != null)
                        {
                            await ioContext.AddTraceMessage(result.Exception.ToString()).ConfigureAwait(false);
                        }
                    }
                }

                if (commandDescription.ModifiesEnvironment && childEnv.HasChanged)
                {
                    environmentContext.UpdateEnvironment(childEnv.GetEnvironment());
                }
            }

            success = resultFailures.Count == 0;
        }
        catch (Exception ex)
        {
            success = false;
            errorMessage = ex.Message;
            await ioContext.OutputChunk(CommandResult<string>.Failure($"Error executing {commandKey} (see trace for more info)", ex)).ConfigureAwait(false);
            await ioContext.SetStatusMessage("**Error: " + ex.Message).ConfigureAwait(false);
            await ioContext.AddTraceMessage(ex.ToString()).ConfigureAwait(false);
        }
        finally
        {
            await ioContext.AddTraceMessage($"ExecuteCommand: {commandKey} Done.").ConfigureAwait(false);

            if (!success && string.IsNullOrEmpty(errorMessage) && resultFailures.Count > 0)
            {
                errorMessage = string.Join(Environment.NewLine, resultFailures);
            }

            var duration = DateTime.UtcNow - startTime;

            var auditEvent = new AuditEvent
            {
                CommandName = commandKey,
                PackageOrigin = commandDescription?.PackageDescription?.FullPath ?? "built-in",
                Parameters = ioContext.Parameters ?? Array.Empty<string>(),
                ExecutedAt = startTime,
                Duration = duration,
                Success = success,
                ErrorMessage = errorMessage,
                PipelineStage = ioContext.PipelineStage,
                PipelineTotalStages = ioContext.PipelineTotalStages
            };
            _auditLogger?.LogAuditEvent(auditEvent);
        }
    }

    private async Task OutputOneLineHelp(ICommandDescription description, IIoContext context)
    {
        if (string.IsNullOrEmpty(description.FullTypeName))
        {
            if (description.SubCommands.Count > 0)
            {
                var subCmd = _commandFactory.CreateCommand(description.SubCommands.First().Value, context);

                if (Attribute.GetCustomAttribute(subCmd.GetType(), typeof(CommandRootAttribute)) is CommandRootAttribute rootAttribute)
                {
                    await context.OutputChunk(CommandResult<string>.Success($"{rootAttribute.Command,-12} {rootAttribute.Description}")).ConfigureAwait(false);
                }

                foreach (var subCommand in description.SubCommands)
                {
                    var helpLine = _helpService.BuildOneLineHelp(subCommand.Value);
                    await context.OutputChunk(CommandResult<string>.Success(helpLine)).ConfigureAwait(false);
                }
            }
            else
            {
                await context.AddTraceMessage($"No type name registered for command: {description.BaseCommand}").ConfigureAwait(false);
            }
        }
        else
        {
            var helpLine = _helpService.BuildOneLineHelp(description);
            await context.OutputChunk(CommandResult<string>.Success(helpLine)).ConfigureAwait(false);
        }
    }
}
