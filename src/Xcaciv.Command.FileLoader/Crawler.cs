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
        Trace.WriteLine($"[Xcaciv.Loader] Crawler security policy set to: {policy}");
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
                            var commandParameters = new CommandParameters();
                            var newDescription = commandParameters.CreatePackageDescription(commandType, packagDesc);

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
                            Trace.WriteLine($"[Xcaciv.Loader] Error processing command type [{commandType.FullName}] in package [{key}]: {ex.Message}");
                        }
                    }

                    packagDesc.Commands = commands;
                }
            }
            catch (SecurityException ex)
            {
                // Xcaciv.Loader 2.1.1: Enhanced security exception handling
                Trace.WriteLine($"[Xcaciv.Loader] Security violation loading package [{key}] from [{binPath}]: " +
                    $"SecurityPolicy={_securityPolicy}, " +
                    $"BasePathRestriction={Path.GetDirectoryName(binPath)}. " +
                    $"Details: {ex.Message}");
                return; // Skip this package due to security violation
            }
            catch (FileNotFoundException ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader] Assembly not found for package [{key}] at [{binPath}]: {ex.Message}");
                return;
            }
            catch (FileLoadException ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader] Failed to load assembly for package [{key}] from [{binPath}]: {ex.Message}");
                return;
            }
            catch (BadImageFormatException ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader] Invalid assembly format for package [{key}] at [{binPath}]: {ex.Message}");
                return;
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                // Xcaciv.Loader: Handle type loading failures (e.g., version mismatches, missing dependencies)
                var loaderMessages = ex.LoaderExceptions?
                    .Where(le => le != null)
                    .Select(le => le!.Message)
                    .Distinct()
                    .ToList() ?? new List<string>();
                
                var detailMessage = loaderMessages.Any() 
                    ? string.Join("; ", loaderMessages)
                    : "No detailed loader exception information available";
                
                Trace.WriteLine($"[Xcaciv.Loader] Type load failure for package [{key}] at [{binPath}]: " +
                    $"Unable to load one or more types. This typically indicates the plugin was compiled against " +
                    $"a different version of the interface assemblies. Details: {detailMessage}");
                return; // Skip this package due to type load failure
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[Xcaciv.Loader] Unexpected error loading package [{key}] from [{binPath}]: {ex.GetType().Name}: {ex.Message}");
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
        // Path.Combine would discard the package directory for a rooted sub-directory
        if (!String.IsNullOrEmpty(subDirectory) && fileSystem.Path.IsPathRooted(subDirectory))
            throw new ArgumentException($"Sub-directory '{subDirectory}' must be relative to each package directory.", nameof(subDirectory));

        // A search mask such as "*\bin\*.dll" must not be passed to Directory.GetFiles: a real
        // file system treats everything before the last separator as a literal directory name
        // and throws DirectoryNotFoundException for "<basePath>\*". Enumerate the documented
        // layout explicitly instead: <basePath>\<Package>\<subDirectory>\*.dll
        var binaryCommandCollections = String.IsNullOrEmpty(subDirectory)
            ? this.fileSystem.Directory.GetFiles(basePath, SearchPattern, SearchOption.AllDirectories)
            : GetPackageBinaryFiles(basePath, subDirectory);

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
    /// enumerate the binaries of every package that follows the documented layout
    /// &lt;basePath&gt;/&lt;Package&gt;/&lt;subDirectory&gt;/*.dll
    /// </summary>
    /// <param name="basePath">resolved, existing base directory</param>
    /// <param name="subDirectory">directory expected under each package directory</param>
    private string[] GetPackageBinaryFiles(string basePath, string subDirectory)
    {
        var results = new List<string>();

        foreach (var packageDirectory in this.fileSystem.Directory.GetDirectories(basePath))
        {
            var binaryDirectory = this.fileSystem.Path.Combine(packageDirectory, subDirectory);
            if (!this.fileSystem.Directory.Exists(binaryDirectory)) continue;

            try
            {
                results.AddRange(this.fileSystem.Directory.GetFiles(binaryDirectory, SearchPattern, SearchOption.AllDirectories));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // one unreadable package must not hide the others, matching how LoadPackageDescriptions treats a bad package
                Trace.WriteLine($"[Xcaciv.Loader] Skipping package directory [{binaryDirectory}]: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return results.ToArray();
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
            var directoryPath = fileSystem.Path.GetDirectoryName(packageFilePath);
            var uniqueId = string.Empty;
            
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                uniqueId = directoryPath.Substring(basePath.Length).Replace(fileSystem.Path.DirectorySeparatorChar.ToString(), String.Empty);
            }
            
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
            var directoryPath = fileSystem.Path.GetDirectoryName(packageFilePath);
            var uniqueId = string.Empty;
            
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                uniqueId = directoryPath.Substring(basePath.Length).Replace(fileSystem.Path.DirectorySeparatorChar.ToString(), String.Empty);
            }
            
            var packageName = $"{fileName}-{uniqueId}";
            if (fileSystem.File.Exists(packageFilePath)) packageAction(packageName, packageFilePath);
        });
    }
}
