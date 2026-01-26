using System.Text.RegularExpressions;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command;

/// <summary>
/// Information about a command and how to execute it
/// </summary>
public class CommandDescription : ICommandDescription
{
    /// <summary>
    /// sanitized internal command text
    /// </summary>
    protected string command = string.Empty;
    
    /// <summary>
    /// text command
    /// </summary>
    public string BaseCommand { get => command; set => command = NamesValidator.GetValidCommandName(value); }
    /// <summary>
    /// sub command text
    /// used to limit the secondary command text
    /// </summary>
    public Dictionary<string, ICommandDescription> SubCommands { get; set; } = new Dictionary<string, ICommandDescription>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Fully Namespaced Type Name
    /// </summary>
    public string FullTypeName { get; set; } = "";
    /// <summary>
    /// full path to containing assembly
    /// </summary>
    public PackageDescription PackageDescription { get; set; } = new PackageDescription();
    /// <summary>
    /// explicitly indicates if a command modifes the environment
    /// </summary>
    public bool ModifiesEnvironment { get; set; }
}
