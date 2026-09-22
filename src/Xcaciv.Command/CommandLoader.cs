using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Exceptions;

namespace Xcaciv.Command;

/// <summary>
/// Handles discovery of commands from verified package directories.
/// </summary>
public class CommandLoader : ICommandLoader
{
    private readonly ICrawler _crawler;
    private readonly IVerifiedSourceDirectories _verifiedDirectories;

    public CommandLoader(ICrawler crawler, IVerifiedSourceDirectories verifiedDirectories)
    {
        _crawler = crawler ?? throw new ArgumentNullException(nameof(crawler));
        _verifiedDirectories = verifiedDirectories ?? throw new ArgumentNullException(nameof(verifiedDirectories));
    }

    public IReadOnlyList<string> Directories => _verifiedDirectories.Directories;

    public void AddPackageDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Directory is required", nameof(directory));

        if (_verifiedDirectories.AddDirectory(directory)) return;

        // AddDirectory only says that it refused. Work out which check failed so the caller
        // gets the reason now, instead of a misleading "No base package directory configured"
        // from LoadCommands later.
        if (!_verifiedDirectories.VerifyRestrictedPath(directory))
        {
            var restriction = string.IsNullOrEmpty(_verifiedDirectories.RestrictedDirectory)
                ? Directory.GetCurrentDirectory()
                : _verifiedDirectories.RestrictedDirectory;

            throw new NoPackageDirectoryFoundException(
                $"Package directory '{directory}' was not added because it is outside the restricted directory '{restriction}'.");
        }

        if (_verifiedDirectories.VerifyFile(directory))
        {
            throw new NoPackageDirectoryFoundException(
                $"Package directory '{directory}' was not added because it is a file, not a directory.");
        }

        throw new NoPackageDirectoryFoundException(
            $"Package directory '{directory}' was not added because it does not exist.");
    }

    public void SetRestrictedDirectory(string restrictedDirectory)
    {
        if (string.IsNullOrWhiteSpace(restrictedDirectory)) throw new ArgumentException("Restricted directory is required", nameof(restrictedDirectory));
        _verifiedDirectories.SetRestrictedDirectory(restrictedDirectory);
    }

    public void LoadCommands(string subDirectory, Action<ICommandDescription> registerCommand)
    {
        if (registerCommand == null) throw new ArgumentNullException(nameof(registerCommand));

        if (_verifiedDirectories.Directories.Count == 0)
        {
            throw new NoPluginsFoundException("No base package directory configured. (Did you set the restricted directory?)");
        }

        foreach (var directory in _verifiedDirectories.Directories)
        {
            var packages = _crawler.LoadPackageDescriptions(directory, subDirectory);
            foreach (var command in packages.SelectMany(o => o.Value.Commands))
            {
                registerCommand(command.Value);
            }
        }
    }
}
