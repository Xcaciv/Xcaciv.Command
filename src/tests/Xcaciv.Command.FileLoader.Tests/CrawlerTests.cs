using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface;
using Xunit;
using Xunit.Abstractions;

namespace Xcaciv.Command.FileLoaderTests;

public class CrawlerTests
{
    private ITestOutputHelper _testOutput;
    private string commandPackageDir = @"..\..\..\..\zTestCommandPackage\bin\{1}\net10.0\";

    public CrawlerTests(ITestOutputHelper output)
    {
        this._testOutput = output;

        // Detect the target framework at runtime
        var targetFramework = GetTargetFramework();
        this._testOutput.WriteLine($"Tests running on {targetFramework}");

        var buildMode = "Debug"; // Default to Debug

#if DEBUG
            this._testOutput.WriteLine("Tests in Debug mode");
#else
        this._testOutput.WriteLine("Tests in Release mode");
        buildMode = "Release";
#endif
        // Build paths using the detected framework
        this.commandPackageDir = $@"..\..\..\..\zTestCommandPackage\bin\{buildMode}\{targetFramework}\";
        this.commandPackageDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.commandPackageDir));
    }

    /// <summary>
    /// Detects the target framework of the current assembly (net8.0, net10.0, etc.)
    /// </summary>
    private static string GetTargetFramework()
    {
        var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

        if (targetFrameworkAttribute != null)
        {
            var frameworkName = targetFrameworkAttribute.FrameworkName;
            // Format: ".NETCoreApp,Version=v10.0" -> "net10.0"
            if (frameworkName.Contains("Version=v"))
            {
                var version = frameworkName.Split("Version=v")[1];
                return $"net{version}";
            }
        }

        // Fallback to net10.0 if detection fails
        return "net10.0";
    }

    private static string basePath = @"C:\Program\Commands\";
    private static string subDirectory = "bin";

    private IFileSystem getFileSystem()
    {
        var filesSystem = new MockFileSystem(new Dictionary<string, MockFileData>() {
                {$@"{basePath}Stuff\{subDirectory}readme.txt", new MockFileData("Testing is meh.") },
                {$@"{basePath}Hello\{subDirectory}\Hello.dll", new MockFileData(Resource1.zTestAssembly) },
                {$@"{basePath}Hello\{subDirectory}\zTestInterfaces.dll", new MockFileData(Resource1.zTestInterfaces) },
                {$@"{basePath}Say\readme.txt", new MockFileData("Testing is meh.") },
                {$@"{basePath}Do\readme.txt", new MockFileData("Testing is meh.") },
                {$@"{basePath}long\deep\{subDirectory}readme.txt", new MockFileData("Testing is meh.") },
                {$@"{basePath}no\return\{subDirectory}\Hello.dll", new MockFileData(Resource1.zTestAssembly) },
                {$@"{basePath}too\deep\{subDirectory}\zTestInterfaces.dll", new MockFileData(Resource1.zTestInterfaces) },
                {$@"{basePath}Root\Hello\{subDirectory}\RootHello.dll", new MockFileData(Resource1.zTestAssembly) },
                {$@"{basePath}Root\Hello\{subDirectory}\zTestInterfaces.dll", new MockFileData(Resource1.zTestInterfaces) },
                {$@"{basePath}Xc.Hello\{subDirectory}\Xc.Hello.dll", new MockFileData(Resource1.zTestAssembly) },
            });
        filesSystem.AddDirectory($@"{basePath}Hello");
        filesSystem.AddDirectory($@"{basePath}Say");
        filesSystem.AddDirectory($@"{basePath}Do");
        filesSystem.AddDirectory($@"{basePath}Stuff");

        return filesSystem;
    }

    [Fact()]
    public void WalkPackagePathsTest1()
    {
        var fileSystem = this.getFileSystem();
        var crawler = new Crawler(fileSystem);

        var paths = new Dictionary<string, string>();
        crawler.CrawlPackagePaths(basePath, subDirectory, (name, binPath) => paths.Add(name, binPath));

        Assert.Equal("C:\\Program\\Commands\\Hello\\bin\\Hello.dll", paths.FirstOrDefault().Value);
    }

    [Fact()]
    public void LoadPackageDescriptionsTest()
    {
        IFileSystem fileSystem = new FileSystem();
        var crawler = new Crawler(fileSystem);
        var packages = crawler.LoadPackageDescriptions(commandPackageDir, String.Empty);

        Assert.True(packages.Where(p => p.Value.Commands.ContainsKey("ECHO")).Any());
    }

    [Fact()]
    public void CrawlPackagePaths_ThrowsDirectoryNotFoundException()
    {
        var fileSystem = new MockFileSystem();
        var crawler = new Crawler(fileSystem);

        Assert.Throws<DirectoryNotFoundException>(() => crawler.CrawlPackagePaths(basePath, subDirectory, (name, binPath) => { }));
    }

    [Fact()]
    public void CrawlPackagePaths_ThrowsNoPackageDirectoryFoundException()
    {
        var fileSystem = new MockFileSystem();
        var crawler = new Crawler(fileSystem);

        fileSystem.AddDirectory(basePath);

        Assert.Throws<Interface.Exceptions.NoPackageDirectoryFoundException>(() => crawler.CrawlPackagePaths(basePath, subDirectory, (name, binPath) => { }));
    }

}
