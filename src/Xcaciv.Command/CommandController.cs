using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Xcaciv.Command.Commands;
using Xcaciv.Command.Core;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command;

/// <summary>
/// Orchestrates command execution, plugin loading, and pipeline management.
/// Serves as the central controller for discovering, registering, and executing commands
/// (both built-in and from plugin packages).
/// </summary>
/// <remarks>
/// The CommandController is the primary entry point for command execution. It manages:
/// - Command registration from plugin packages (via Crawler)
/// - Command instantiation and execution
/// - Pipeline support for chaining multiple commands
/// - Audit logging for command execution and environment changes
///
/// Security: Commands are loaded from verified package directories only (see PackageBinaryDirectories).
/// Plugin assembly loading uses AssemblyContext with security restrictions.
/// </remarks>
public class CommandController : Interface.ICommandController
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly ICommandLoader _commandLoader;
    private readonly IPipelineExecutor _pipelineExecutor;
    private readonly ICommandExecutor _commandExecutor;
    private readonly ICommandFactory _commandFactory;
    private readonly IHelpService _helpService;

    protected ICommandRegistry CommandRegistry => _commandRegistry;

    private string _helpCommand = "HELP";
    private IAuditLogger _auditLogger;
    private IOutputEncoder _outputEncoder;

    /// <summary>
    /// Command Manager default constructor.
    /// </summary>
    public CommandController()
        : this(null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Command Manager constructor allowing a custom crawler.
    /// </summary>
    /// <param name="crawler">The ICrawler implementation for plugin discovery.</param>
    public CommandController(ICrawler crawler)
        : this(
            commandRegistry: null,
            commandLoader: new CommandLoader(crawler, new VerifiedSourceDirectories(new FileSystem())),
            pipelineExecutor: null,
            commandExecutor: null,
            commandFactory: null,
            serviceProvider: null)
    {
    }

    /// <summary>
    /// Command Manager constructor to specify restricted directory.
    /// </summary>
    /// <param name="crawler">Crawler used for plugin discovery.</param>
    /// <param name="restrictedDirectory">Base directory for plugin packages.</param>
    public CommandController(ICrawler crawler, string restrictedDirectory)
        : this(crawler)
    {
        _commandLoader.SetRestrictedDirectory(restrictedDirectory);
    }

    /// <summary>
    /// Command Manager constructor for testing custom directory verification logic.
    /// </summary>
    /// <param name="packageBinaryDirectories">Custom implementation of IVerifiedSourceDirectories.</param>
    public CommandController(IVerifiedSourceDirectories packageBinaryDirectories)
        : this(
            commandRegistry: null,
            commandLoader: new CommandLoader(new Crawler(), packageBinaryDirectories),
            pipelineExecutor: null,
            commandExecutor: null,
            commandFactory: null,
            serviceProvider: null)
    {
    }

    /// <summary>
    /// Dependency-injection friendly constructor for supplying orchestrator components.
    /// </summary>
    public CommandController(
        ICommandRegistry? commandRegistry,
        ICommandLoader? commandLoader,
        IPipelineExecutor? pipelineExecutor,
        ICommandExecutor? commandExecutor,
        ICommandFactory? commandFactory,
        IServiceProvider? serviceProvider)
    {
        _commandRegistry = commandRegistry ?? new CommandRegistry();
        _commandFactory = commandFactory ?? new CommandFactory(serviceProvider);
        _helpService = new HelpService();
        _commandExecutor = commandExecutor ?? new CommandExecutor(_commandRegistry, _commandFactory, _helpService);
        _commandLoader = commandLoader ?? new CommandLoader(new Crawler(), new VerifiedSourceDirectories(new FileSystem()));
        _pipelineExecutor = pipelineExecutor ?? new PipelineExecutor();

        _auditLogger = new NoOpAuditLogger();
        _outputEncoder = new NoOpEncoder();

        _commandExecutor.AuditLogger = _auditLogger;
        _commandExecutor.HelpCommand = _helpCommand;
    }

    /// <summary>
    /// Gets or sets the command name for the help command.
    /// </summary>
    public string HelpCommand
    {
        get => _helpCommand;
        set
        {
            _helpCommand = string.IsNullOrWhiteSpace(value) ? "HELP" : NamesValidator.GetValidCommandName(value);
            _commandExecutor.HelpCommand = _helpCommand;
        }
    }

    /// <summary>
    /// Gets or sets the optional audit logger for command execution and environment changes.
    /// If not provided, defaults to NoOpAuditLogger (no logging).
    /// </summary>
    public IAuditLogger AuditLogger
    {
        get => _auditLogger;
        set
        {
            _auditLogger = value ?? new NoOpAuditLogger();
            _commandExecutor.AuditLogger = _auditLogger;
        }
    }

    /// <summary>
    /// Gets or sets the output encoder for encoding command output for specific target systems.
    /// </summary>
    public IOutputEncoder OutputEncoder
    {
        get => _outputEncoder;
        set => _outputEncoder = value ?? new NoOpEncoder();
    }

    /// <summary>
    /// Gets or sets the pipeline configuration for DoS protection.
    /// </summary>
    public PipelineConfiguration PipelineConfig
    {
        get => _pipelineExecutor.Configuration;
        set => _pipelineExecutor.Configuration = value ?? throw new ArgumentNullException(nameof(PipelineConfig));
    }

    /// <summary>
    /// add a base directory for loading command packages
    /// </summary>
    /// <param name="directory">Base directory path containing plugin subdirectories.</param>
    public void AddPackageDirectory(string directory)
    {
        _commandLoader.AddPackageDirectory(directory);
    }

    /// <summary>
    /// (re)populate command collection using a crawler
    /// </summary>
    /// <param name="subDirectory">Subdirectory name where compiled plugins are located (default: "bin").</param>
    public void LoadCommands(string subDirectory = "bin")
    {
        _commandLoader.LoadCommands(subDirectory, _commandRegistry.AddCommand);
    }

    /// <summary>
    /// Registers all built-in commands (Say, Set, Env, Regif).
    /// </summary>
    public void RegisterBuiltInCommands()
    {
        var packageKey = "Default";

        AddCommand(packageKey, new RegifCommand());
        AddCommand(packageKey, new SayCommand());
        AddCommand(packageKey, new SetCommand(), true);
        AddCommand(packageKey, new EnvCommand());
    }

    /// <summary>
    /// Registers a command instance with the controller.
    /// </summary>
    public void AddCommand(string packageKey, ICommandDelegate command, bool modifiesEnvironment = false)
    {
        _commandRegistry.AddCommand(packageKey, command, modifiesEnvironment);
    }

    /// <summary>
    /// Registers a command type with the controller.
    /// </summary>
    public void AddCommand(string packageKey, Type commandType, bool modifiesEnvironment = false)
    {
        _commandRegistry.AddCommand(packageKey, commandType, modifiesEnvironment);
    }

    /// <summary>
    /// install a single command into the index
    /// </summary>
    public void AddCommand(ICommandDescription command)
    {
        _commandRegistry.AddCommand(command);
    }

    /// <summary>
    /// parse a command line, find and execute the command passing in the arguments
    /// </summary>
    public async Task Run(string commandLine, IIoContext ioContext, IControllerEnvironmentContext env)
    {
        await Run(commandLine, ioContext, env, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task Run(string commandLine, IIoContext ioContext, IEnvironmentContext env)
    {
        await Run(commandLine, ioContext, env, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task Run(string commandLine, IIoContext ioContext, IEnvironmentContext env, CancellationToken cancellationToken)
    {
        if (env == null) throw new ArgumentNullException(nameof(env));
        var controllerEnv = env as IControllerEnvironmentContext ?? new ControllerEnvironmentContext(env);
        await Run(commandLine, ioContext, controllerEnv, cancellationToken).ConfigureAwait(false);
    }

    public async Task Run(string commandText, IIoContext ioContext, IControllerEnvironmentContext env, CancellationToken cancellationToken)
    {
        if (commandText == null) throw new ArgumentNullException(nameof(commandText));
        if (ioContext == null) throw new ArgumentNullException(nameof(ioContext));
        if (env == null) throw new ArgumentNullException(nameof(env));

        cancellationToken.ThrowIfCancellationRequested();

        env.SetAuditLogger(_auditLogger);

        ioContext.SetOutputEncoder(_outputEncoder);

        if (commandText.IndexOf(CommandSyntax.PipelineDelimiter) >= 0)
        {
            var controllerEnvironmentChild = await env.GetChild();
            await _pipelineExecutor.ExecuteAsync(commandText, ioContext, controllerEnvironmentChild, ExecuteCommandInternal, cancellationToken).ConfigureAwait(false);
            var lastCommandName = _pipelineExecutor.LastStageCommand;

            if (controllerEnvironmentChild.HasChanged && _commandRegistry.TryGetCommand(lastCommandName, out var commandDesc) && commandDesc?.ModifiesEnvironment == true)
            {
                var modifiedEnv = controllerEnvironmentChild.GetEnvironment(lastCommandName, false);
                env.UpdateEnvironment(modifiedEnv);
            }
        }
        else
        {
            var commandName = NamesValidator.GetValidCommandName(commandText);
            var args = NamesValidator.GetArgumentsFromCommandline(commandText);
            await ioContext.SetParameters([.. args]).ConfigureAwait(false);

            var childEnv = await env.GetChild(commandName).ConfigureAwait(false);
            await ExecuteCommandInternal(commandName, ioContext, childEnv, cancellationToken).ConfigureAwait(false);
            if (childEnv.HasChanged && _commandRegistry.TryGetCommand(commandName, out var commandDesc))
            {
                // if this is a special command, it can update global values
                if (commandDesc?.ModifiesEnvironment == true)
                {
                    env.UpdateEnvironment(childEnv.GetEnvironment());
                }
                else
                {
                    // only allow commands to update their own environment values
                    env.UpdateEnvironment(childEnv.GetEnvironment(), commandName);
                }
            }
        }
    }

    private async Task ExecuteCommandInternal(string commandKey, IIoContext ioContext, IEnvironmentContext env, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = ioContext.Parameters ?? Array.Empty<string>();
        if (IsHelpRequest(commandKey, parameters))
        {
            await HandleHelpRequest(commandKey, ioContext, env, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _commandExecutor.ExecuteAsync(commandKey, ioContext, env, cancellationToken).ConfigureAwait(false);
    }

    private bool IsHelpRequest(string commandKey, string[] parameters)
    {
        if (commandKey.Equals(_helpCommand, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _helpService.IsHelpRequest(parameters);
    }

    private Task HandleHelpRequest(string commandKey, IIoContext ioContext, IEnvironmentContext env, CancellationToken cancellationToken)
    {
        var parameters = ioContext.Parameters ?? Array.Empty<string>();
        var targetCommand = commandKey.Equals(_helpCommand, StringComparison.OrdinalIgnoreCase)
            ? (parameters.Length > 0 ? parameters[0] : string.Empty)
            : commandKey;

        return _commandExecutor.GetHelpAsync(targetCommand, ioContext, env, cancellationToken);
    }

    /// <summary>
    /// Gets the current environment context for the controller.
    /// </summary>
    /// <returns>An instance of <see cref="IControllerEnvironmentContext"/> representing the default environment context given the commands registered.</returns>
    public IControllerEnvironmentContext GetEnvironment()
    {
        return _commandRegistry.GetEnvironment(_commandFactory);
    }
}
