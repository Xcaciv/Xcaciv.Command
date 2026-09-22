using System.IO.Abstractions;

namespace Xcaciv.Command.FileLoader;

public class VerifiedSourceDirectories : IVerifiedSourceDirectories
{
    protected IFileSystem FileSystem { get; set; }
    /// <summary>
    /// directories to search for command assemblies
    /// </summary>
    protected List<string> commandDirectories = new List<string>();
    /// <summary>
    /// get a copy of command direcory search list
    /// </summary>
    public IReadOnlyList<string> Directories => commandDirectories;

    public string RestrictedDirectory { get; private set; } = string.Empty;

    /// <summary>
    /// constructor defaulting the file system abstraction
    /// </summary>
    public VerifiedSourceDirectories() : this(new FileSystem()) { }
    public VerifiedSourceDirectories(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }
    /// <summary>
    /// restrict file path and translat to fully qualified file name
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="restrictedPath"></param>
    /// <param name="shouldThrow"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public bool VerifyRestrictedPath(string filePath, bool shouldThrow = false)
    {
        var pathWrapper = new PathWrapper(FileSystem);
        return VerifyRestrictedPath(pathWrapper, filePath, this.RestrictedDirectory, shouldThrow);

    }
    /// <summary>
    /// confirm a file exists within the restricted path
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="restrictedPath"></param>
    /// <param name="shouldThrow"></param>
    /// <returns></returns>
    public bool VerifyFile(string filePath, bool shouldThrow = false)
    {
        if (VerifyRestrictedPath(filePath, shouldThrow) && FileSystem.File.Exists(filePath))
        {
            return !FileSystem.File.GetAttributes(filePath)
                .HasFlag(FileAttributes.Directory);
        }

        return false;
    }
    /// <summary>
    /// confirm a directory exists within a restricted path
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="restrictedPath"></param>
    /// <param name="shouldThrow"></param>
    /// <returns></returns>
    public bool VerifyDirectory(string filePath, bool shouldThrow = false)
    {
        if (VerifyRestrictedPath(filePath, shouldThrow) && FileSystem.Directory.Exists(filePath))
        {
            return FileSystem.File.GetAttributes(filePath)
                .HasFlag(FileAttributes.Directory);
        }

        return false;
    }
    /// <summary>
    /// add to the command directory search list
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="restrictedPath"></param>
    /// <returns></returns>
    public bool AddDirectory(string directory)
    {
        if (!VerifyDirectory(directory)) return false;

        commandDirectories.Add(directory);

        return true;
    }
    /// <summary>
    /// confirm that a path resolves as a child of a restricted directory
    /// </summary>
    /// <param name="pathWrapper"></param>
    /// <param name="filePath"></param>
    /// <param name="restrictedPath"></param>
    /// <param name="shouldThrow"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool VerifyRestrictedPath(IPath pathWrapper, string filePath, string? restrictedPath = null, bool shouldThrow = false)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));
        if (String.IsNullOrEmpty(restrictedPath)) restrictedPath = Directory.GetCurrentDirectory();

        // Compare the path itself, not its parent, and compare as strings rather than with
        // Uri.IsBaseOf: a Uri without a trailing separator is treated as its parent directory,
        // which let paths beside the restricted directory pass. A path is inside the restricted
        // directory when it is that directory or starts with it followed by a separator.
        var fullPath = pathWrapper.TrimEndingDirectorySeparator(pathWrapper.GetFullPath(filePath));
        var fullRestrictedPath = pathWrapper.TrimEndingDirectorySeparator(pathWrapper.GetFullPath(restrictedPath));
        var restrictedPrefix = pathWrapper.EndsInDirectorySeparator(fullRestrictedPath)
            ? fullRestrictedPath // a root such as "/" or "C:\" keeps its separator
            : fullRestrictedPath + pathWrapper.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var isInside = String.Equals(fullPath, fullRestrictedPath, comparison)
            || fullPath.StartsWith(restrictedPrefix, comparison);

        if (!isInside)
        {
            if (shouldThrow) throw new ArgumentOutOfRangeException(nameof(filePath) + " must be located within " + nameof(restrictedPath));
            else return false;
        }

        return true;
    }
    /// <summary>
    /// set instance restricted directory
    /// </summary>
    /// <param name="restrictedDirectory"></param>
    public void SetRestrictedDirectory(string restrictedDirectory)
    {
        this.RestrictedDirectory = restrictedDirectory;
    }
}
