
namespace Xcaciv.Command.FileLoader;

public interface IVerifiedSourceDirectories
{
    IReadOnlyList<string> Directories { get; }
    string RestrictedDirectory { get; }
    bool AddDirectory(string directory);
    bool VerifyDirectory(string filePath, bool shouldThrow = false);
    bool VerifyFile(string filePath, bool shouldThrow = false);
    bool VerifyRestrictedPath(string filePath, bool shouldThrow = false);
    void SetRestrictedDirectory(string restrictedDirectory);
}
