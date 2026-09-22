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

    /// <summary>
    /// Regression: the crawler used to pass a search mask such as "*\bin\*.dll" straight to
    /// Directory.GetFiles. A real file system treats everything before the last separator in a
    /// search pattern as a literal directory name, so it tried to open a directory named "*" and
    /// threw DirectoryNotFoundException. MockFileSystem accepts the mask as a glob, which is why
    /// the other tests in this class never caught it. This test uses a real temp directory laid
    /// out per the documented convention: &lt;base&gt;/&lt;Package&gt;/bin/&lt;Package&gt;.dll
    /// </summary>
    [Fact()]
    public void CrawlPackagePaths_RealFileSystem_DocumentedLayout_FindsPackageDll()
    {
        var realBasePath = Path.Combine(Path.GetTempPath(), "XcacivCrawlerRealFsTest_" + Guid.NewGuid().ToString("N"));
        var packageBinDir = Path.Combine(realBasePath, "PkgA", "bin");
        Directory.CreateDirectory(packageBinDir);
        var dllPath = Path.Combine(packageBinDir, "PkgA.dll");
        // CrawlPackagePaths only enumerates and checks File.Exists; it never loads the assembly.
        File.WriteAllBytes(dllPath, new byte[] { 0x4D, 0x5A });

        try
        {
            var crawler = new Crawler(); // real file system
            var paths = new Dictionary<string, string>();

            crawler.CrawlPackagePaths(realBasePath, "bin", (name, binPath) => paths.Add(name, binPath));

            Assert.Single(paths);
            Assert.Equal(dllPath, paths.Values.First());
        }
        finally
        {
            if (Directory.Exists(realBasePath)) Directory.Delete(realBasePath, recursive: true);
        }
    }

    /// <summary>
    /// Path.Combine discards the package directory when subDirectory is rooted, which would
    /// make every package probe the same absolute path. Reject it up front.
    /// </summary>
    [Fact()]
    public void CrawlPackagePaths_RootedSubDirectory_ThrowsArgumentException()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(basePath);
        var crawler = new Crawler(fileSystem);
        var rooted = fileSystem.Path.DirectorySeparatorChar + "bin";

        Assert.Throws<ArgumentException>(() => crawler.CrawlPackagePaths(basePath, rooted, (name, binPath) => { }));
    }

    /// <summary>
    /// One package whose bin tree cannot be read must not stop the other packages from being
    /// found. Skipped on Windows, where chmod has no effect.
    /// </summary>
    [Fact()]
    public void CrawlPackagePaths_RealFileSystem_UnreadablePackage_OtherPackagesStillFound()
    {
        if (OperatingSystem.IsWindows()) return;

        var realBasePath = Path.Combine(Path.GetTempPath(), "XcacivCrawlerRealFsTest_" + Guid.NewGuid().ToString("N"));
        var goodDll = Path.Combine(realBasePath, "PkgGood", "bin", "PkgGood.dll");
        var lockedDir = Path.Combine(realBasePath, "PkgBad", "bin", "locked");
        Directory.CreateDirectory(Path.GetDirectoryName(goodDll)!);
        File.WriteAllBytes(goodDll, new byte[] { 0x4D, 0x5A });
        Directory.CreateDirectory(lockedDir);
        File.SetUnixFileMode(lockedDir, UnixFileMode.None);

        try
        {
            var paths = new Dictionary<string, string>();

            new Crawler().CrawlPackagePaths(realBasePath, "bin", (name, binPath) => paths.Add(name, binPath));

            Assert.Equal(goodDll, Assert.Single(paths).Value);
        }
        finally
        {
            File.SetUnixFileMode(lockedDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            if (Directory.Exists(realBasePath)) Directory.Delete(realBasePath, recursive: true);
        }
    }

}
