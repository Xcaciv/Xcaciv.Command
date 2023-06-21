using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;
using Xcaciv.Loader;

namespace Xcaciv.Command;

/// <summary>
/// Command Manager
/// </summary>
public class CommandController : ICommandController
{
    protected ICrawler Crawler;

    /// <summary>
    /// registered commands
    /// </summary>
    protected Dictionary<string, CommandDescription> Commands { get; set; } = new Dictionary<string, CommandDescription>();
    /// <summary>
    /// restricted directories containing command packages
    /// if no restrictedDirectory is specified the current running directory will be used
    /// </summary>
    protected IVerfiedSourceDirectories PackageBinaryDirectories { get; set; } = new VerfiedSourceDirectories(new FileSystem());
    /// <summary>
    /// Command Manger
    /// </summary>
    public CommandController() : this(new Crawler()) { }
    /// <summary>
    /// Command Manger
    /// </summary>
    public CommandController(ICrawler crawler) 
    {
        this.Crawler = crawler;
    }
    /// <summary>
    /// Command Manager constructor to specify restricted directory
    /// </summary>
    /// <param name="restrictedDirectory"></param>
    public CommandController(ICrawler crawler,string restrictedDirectory) : this(crawler)
    {
        this.PackageBinaryDirectories.SetRestrictedDirectory(restrictedDirectory);
    }
    /// <summary>
    /// Command Manager test constructor
    /// </summary>
    /// <param name="packageBinearyDirectories"></param>
    public CommandController(IVerfiedSourceDirectories packageBinearyDirectories) : this()
    {
        this.PackageBinaryDirectories = packageBinearyDirectories;
    }
    /// <summary>
    /// add a base directory for loading command packages
    /// default directory structure for a plugin will be:
    ///     directory/PluginName/bin/PluginName.dll
    /// </summary>
    /// <param name="directory"></param>
    public void AddPackageDirectory(string directory)
    {
        this.PackageBinaryDirectories.AddDirectory(directory);
    }
    /// <summary>
    /// (re)populate command collection using a crawler
    /// </summary>
    /// <param name="subDirectory"></param>
    /// <exception cref="Exceptions.InValidConfigurationException"></exception>
    public void LoadCommands(string subDirectory = "bin")
    {
        if (this.PackageBinaryDirectories.Directories.Count == 0) throw new Exceptions.NoPluginsFoundException("No base package directory configured.");

        this.Commands.Clear();
        foreach (var directory in this.PackageBinaryDirectories.Directories)
        {
            foreach (var command in Crawler.LoadPackageDescriptions(directory, subDirectory).SelectMany(o => o.Value.Commands))
            {
                this.Commands.Add(command.Value.BaseCommand, command.Value);
            }
        }
    }
    /// <summary>
    /// parse a command line, find and execute the command passing in the arguments
    /// </summary>
    /// <param name="commandLine"></param>
    public async Task Run(string commandLine, ITextIoContext output)
    {
        var commandName = GetCommand(commandLine);
        var args = PrepareArgs(commandLine);
        await Run(commandName, args, output);
    }
    /// <summary>
    /// execute command event
    /// </summary>
    /// <param name="commandKey"></param>
    /// <param name="args"></param>
    /// <param name="ioContext"></param>
    private async Task Run(string commandKey, string[] args, ITextIoContext ioContext)
    {
        if (!this.Commands.ContainsKey(commandKey))
        {
            await ioContext.SetStatusMessage($"Command [{commandKey}] not found.");
            return;
        }
        try
        {
            var commandDiscription = this.Commands[commandKey];
            using (var context = AssemblyContext.LoadFromPath(commandDiscription.PackageDescription.FullPath))
            {
                var commandInstance = context.GetInstance<ICommand>(commandDiscription.FullTypeName);
                await commandInstance.Main(args, ioContext);
            }
        }
        catch (Exception ex)
        {
            await ioContext.SetStatusMessage(ex.ToString());
        }
    }
    /// <summary>
    /// parse primary command from a command line
    /// </summary>
    /// <param name="commandLine">full command line</param>
    /// <returns></returns>
    public static string GetCommand(string commandLine)
    {
        var commandText = commandLine.Substring(0, commandLine.IndexOf(' ')).ToUpper();
        return CommandDescription.InvalidCommandChars.Replace(commandText, "");
    }
    /// <summary>
    /// parses arguments from a command line
    /// </summary>
    /// <param name="commandLine">full command line</param>
    /// <returns></returns>
    public static string[] PrepareArgs(string commandLine)
    {
        var args = System.Text.RegularExpressions.Regex.Matches(commandLine, @"[\""].+?[\""]|[\w-]+")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(o => CommandDescription.InvalidCommandChars.Replace(o.Value, ""))
            .ToArray();

        return args.Skip(1).ToArray();
    }

}
