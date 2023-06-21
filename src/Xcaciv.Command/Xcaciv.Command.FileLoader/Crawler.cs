using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Loader;

namespace Xcaciv.Command.FileLoader;

public class Crawler : ICrawler
{
    /// <summary>
    /// number of package dlls signaling the need to paralell process
    /// </summary>
    public static int ParallelizeAt { get; set; } = 50;
    /// <summary>
    /// abstraction for file system
    /// </summary>
    protected IFileSystem fileSystem;
    /// <summary>
    /// empty constructor with default IFileSystem
    /// </summary>
    public Crawler() : this(new FileSystem()) { }
    /// <summary>
    /// constructor for testing with test IFileSystem
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public Crawler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }
    /// <summary>
    /// interigate packages for commands and return descriptions
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="subDirectory"></param>
    /// <returns></returns>
    public IDictionary<string, PackageDescription> LoadPackageDescriptions(string basePath, string subDirectory)
    {
        var packages = new ConcurrentDictionary<string, PackageDescription>();

        // use a callback to process listing commands
        this.CrawlPackagePaths(basePath, subDirectory, (name, binPath) =>
        {
            var packagDesc = new PackageDescription()
            {
                Name = name,
                FullPath = binPath,
            };

            using (var context = AssemblyContext.LoadFromPath(binPath))
            {
                var commands = new Dictionary<string, CommandDescription>();
                packagDesc.Version = context.GetVersion();

                foreach (var command in context.GetAllInstances<ICommand>())
                {
                    commands[command.BaseCommand] = new CommandDescription()
                    {
                        BaseCommand = command.BaseCommand,
                        FullTypeName = command.GetType().FullName ?? String.Empty,
                        PackageDescription = packagDesc
                    };
                }

                packagDesc.Commands = commands;
            }

            packages.TryAdd(binPath, packagDesc);
        });

        return packages;
    }

    /// <summary>
    /// walk a set of directories matching a path convention
    /// NOTE: can perform paralell processing if number of directories 
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="subDirectory"></param>
    /// <param name="packageAction">Action(name, path) THREAD SAFE</param>
    public void CrawlPackagePaths(string basePath, string subDirectory, Action<string, string> packageAction)
    {
        basePath = fileSystem.Path.GetFullPath(basePath);
        if (!this.fileSystem.Directory.Exists(basePath)) throw new DirectoryNotFoundException(basePath);

        var binaryDirectories = fileSystem.Directory.GetDirectories(basePath, fileSystem.Path.Combine("*", subDirectory),
                SearchOption.AllDirectories);

        if (!binaryDirectories.Any()) throw new Exceptions.NoPackageDirectoryFoundException($"No packages found in {basePath}.");

        // avoid overhead of paralell if it is not needed
        if (binaryDirectories.Count() > ParallelizeAt)
        {
            ForEachDirectoryParallel(basePath, subDirectory, packageAction, binaryDirectories);
        }
        else
        {
            ForEachDirectory(basePath, subDirectory, packageAction, binaryDirectories);
        }
    }
    /// <summary>
    /// liniar direcory processing using supplied action
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="subDirectory"></param>
    /// <param name="packageAction"></param>
    /// <param name="binaryDirectories"></param>
    protected void ForEachDirectory(string basePath, string subDirectory, Action<string, string> packageAction, string[] binaryDirectories)
    {
        foreach (var directory in binaryDirectories)
        {
            var packageName = directory.Remove(0, basePath.Length).Replace(subDirectory, String.Empty).Replace(@"\", String.Empty);
            var packageFilePath = fileSystem.Path.Combine(directory, packageName + ".dll");

            if (fileSystem.File.Exists(packageFilePath)) packageAction(packageName, packageFilePath);
        }
    }
    /// <summary>
    /// parallel direcory processing using supplied action
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="subDirectory"></param>
    /// <param name="packageAction"></param>
    /// <param name="binaryDirectories"></param>
    protected void ForEachDirectoryParallel(string basePath, string subDirectory, Action<string, string> packageAction, string[] binaryDirectories)
    {
        Parallel.ForEach(binaryDirectories, (directory) =>
        {
            var packageName = directory.Remove(0, basePath.Length).Replace(subDirectory, String.Empty).Replace(@"\", String.Empty);
            var packageFilePath = fileSystem.Path.Combine(directory, packageName + ".dll");

            if (fileSystem.File.Exists(packageFilePath)) packageAction(packageName, packageFilePath);
        });
    }
}
