using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Security;
using Xcaciv.Command.Core;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;
using Xcaciv.Command.Interface.Exceptions;
using Xcaciv.Loader;

namespace Xcaciv.Command.FileLoader;

public class Crawler : ICrawler
{
    /// <summary>
    /// number of package dlls signaling the need to paralell process
    /// </summary>
    public static int ParallelizeAt { get; set; } = 50;

    private const string SearchPattern = "*.dll";

    /// <summary>
    /// abstraction for file system
    /// </summary>
    protected IFileSystem fileSystem;
    
    /// <summary>
    /// Assembly loading security policy (Xcaciv.Loader 2.1.1 instance-based configuration).
    /// Default: AssemblySecurityPolicy.Strict (requires explicit allowlist and enforces base path restrictions)
    /// </summary>
    private AssemblySecurityPolicy _securityPolicy = AssemblySecurityPolicy.Strict;
    
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
    /// Set the security policy for plugin assembly loading.
    /// Leverages Xcaciv.Loader 2.1.1 instance-based security configuration.
    /// Default: AssemblySecurityPolicy.Strict (path restrictions enforced, explicit allowlists required)
    /// </summary>
    /// <param name="policy">Security policy: Strict (recommended) or Default (legacy)</param>
    public void SetSecurityPolicy(AssemblySecurityPolicy policy)
    {
        _securityPolicy = policy;
        Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Crawler security policy set to: {policy}");
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
        this.CrawlPackagePaths(basePath, subDirectory, (key, binPath) =>
        {
            // This is the package action

            var packagDesc = new PackageDescription()
            {
                Name = key,
                FullPath = binPath,
            };

            try
            {
                // Xcaciv.Loader 2.1.1: Instance-based security with per-plugin path restrictions
                // Each plugin is sandboxed to its own directory, preventing directory traversal attacks
                var basePathRestriction = Path.GetDirectoryName(binPath) ?? Directory.GetCurrentDirectory();
                
                using (var context = new AssemblyContext(
                    binPath,
                    basePathRestriction: basePathRestriction,
                    securityPolicy: _securityPolicy))
                {
                    var commands = new Dictionary<string, ICommandDescription>();
                    packagDesc.Version = context.GetVersion();

                    // Xcaciv.Loader 2.1.1: GetTypes with security policy enforcement
                    foreach (var commandType in context.GetTypes<ICommandDelegate>())
                    {
                        if (commandType == null) continue; // not sure why it could be null, but the compiler says so

                        try
                        {
                            var newDescription = CommandParameters.CreatePackageDescription(commandType, packagDesc);

                            // when it is a sub command, we need to add it to a parent if it already exists
                            if (newDescription.SubCommands.Count > 0 && commands.TryGetValue(newDescription.BaseCommand, out ICommandDescription? description))
                            {
                                var newSubCommand = newDescription.SubCommands.First().Value;
                                description.SubCommands[newSubCommand.BaseCommand] = newSubCommand; 
                            }
                            else
                            {
                                // when the parent command does not exist, add it to the list
                                commands[newDescription.BaseCommand] = newDescription;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Error processing command type [{commandType.FullName}] in package [{key}]: {ex.Message}");
                        }
                    }

                    packagDesc.Commands = commands;
                }
            }
            catch (SecurityException ex)
            {
                // Xcaciv.Loader 2.1.1: Enhanced security exception handling
                Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Security violation loading package [{key}] from [{binPath}]: " +
                    $"SecurityPolicy={_securityPolicy}, " +
                    $"BasePathRestriction={Path.GetDirectoryName(binPath)}. " +
                    $"Details: {ex.Message}");
                return; // Skip this package due to security violation
            }
            catch (FileNotFoundException ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Assembly not found for package [{key}] at [{binPath}]: {ex.Message}");
                return;
            }
            catch (FileLoadException ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Failed to load assembly for package [{key}] from [{binPath}]: {ex.Message}");
                return;
            }
            catch (BadImageFormatException ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Invalid assembly format for package [{key}] at [{binPath}]: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader 2.1.1] Unexpected error loading package [{key}] from [{binPath}]: {ex.GetType().Name}: {ex.Message}");
                return; // Skip this package due to error
            }

            // dont add packages without valid commands
            if (packagDesc.Commands.Count > 0) packages.TryAdd(key, packagDesc);
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
    /// <exception cref="DirectoryNotFoundException">Thrown if the basePath directory does not exist.</exception>
    /// <exception cref="NoPackageDirectoryFoundException">Thrown if no packages are found in the basePath directory.</exception>
    public void CrawlPackagePaths(string basePath, string subDirectory, Action<string, string> packageAction)
    {
        basePath = fileSystem.Path.GetFullPath(basePath);
        if (!this.fileSystem.Directory.Exists(basePath)) throw new DirectoryNotFoundException(basePath);

        string searchMask = (String.IsNullOrEmpty(subDirectory)) ? SearchPattern : fileSystem.Path.Combine("*", subDirectory, SearchPattern);

        var binaryCommandCollections = this.fileSystem.Directory.GetFiles(basePath, searchMask, SearchOption.AllDirectories);

        if (!binaryCommandCollections.Any()) throw new NoPackageDirectoryFoundException($"No packages found in {basePath}.");

        // avoid overhead of paralell if it is not needed
        if (binaryCommandCollections.Count() > ParallelizeAt)
        {
            ForEachDirectoryParallel(basePath, subDirectory, packageAction, binaryCommandCollections);
        }
        else
        {
            ForEachDirectory(basePath, subDirectory, packageAction, binaryCommandCollections);
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
        foreach (var packageFilePath in binaryDirectories)
        {
            var fileName = fileSystem.Path.GetFileNameWithoutExtension(packageFilePath);
            var uniqueId = fileSystem.Path.GetDirectoryName(packageFilePath)?.Remove(0, basePath.Length)?.Replace(@"\", String.Empty);
            var packageName = $"{fileName}-{uniqueId}";
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
        Parallel.ForEach(binaryDirectories, (packageFilePath) =>
        {
            var fileName = fileSystem.Path.GetFileNameWithoutExtension(packageFilePath);
            var uniqueId = fileSystem.Path.GetDirectoryName(packageFilePath)?.Remove(0, basePath.Length)?.Replace(@"\", String.Empty);
            var packageName = $"{fileName}-{uniqueId}";
            if (fileSystem.File.Exists(packageFilePath)) packageAction(packageName, packageFilePath);
        });
    }
}
