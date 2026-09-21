using Xcaciv.Command.Interface;

namespace Xcaciv.Command.FileLoader;

public interface ICrawler
{
    IDictionary<string, PackageDescription> LoadPackageDescriptions(string basePath, string subDirectory);
}
