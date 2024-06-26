using Xcaciv.Command.FileLoader;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Abstractions;
using System.IO;
using Xunit.Abstractions;
using System.Runtime.Serialization;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.FileLoaderTests;

public class CrawlerTests
{
    private ITestOutputHelper _testOutput;
    private string commandPackageDir = @"..\..\..\..\zTestCommandPackage\bin\{1}\";

    public CrawlerTests(ITestOutputHelper output)
    {
        this._testOutput = output;
#if DEBUG
        this._testOutput.WriteLine("Tests in Debug mode");
        this.commandPackageDir = commandPackageDir.Replace("{1}", "Debug");
#else
        this._testOutput.WriteLine("Tests in Release mode??");
        this.commandPackageDir = commandPackageDir.Replace("{1}", "Release");
#endif
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
