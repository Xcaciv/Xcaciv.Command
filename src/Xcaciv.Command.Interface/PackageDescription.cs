namespace Xcaciv.Command.Interface;

public class PackageDescription
{
    /// <summary>
    /// unique name of package
    /// (also used in path and assembly name by convention : name/bin/name.dll
    /// </summary>
    public string Name { get; set; } = String.Empty;
    /// <summary>
    /// sim ver of package for checking for updates
    /// </summary>
    public Version Version { get; set; } = new Version();
    /// <summary>
    /// full path to location of binary
    /// </summary>
    public string FullPath { get; set; } = String.Empty;
    public Dictionary<string, ICommandDescription> Commands { get; set; } = new Dictionary<string, ICommandDescription>();
}
