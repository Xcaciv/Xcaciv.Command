using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xcaciv.Command.FileLoader;
using Xunit;

namespace Xcaciv.Command.Tests
{
    /// <summary>
    /// Regression tests for plugin command classes declared without a namespace.
    /// </summary>
    /// <remarks>
    /// Xcaciv.Loader 2.1.2's AssemblyContext.CreateInstance&lt;T&gt;(string) prepends "." to a
    /// class name that contains no dot and then looks for a type whose FullName ends with it.
    /// That lets a bare class name match a namespaced type, but a type that truly has no
    /// namespace has FullName "NoNamespaceCommand", which never ends with ".NoNamespaceCommand",
    /// so the loader reported the type as missing. CommandFactory now resolves the Type from the
    /// loaded assembly and activates it directly.
    /// </remarks>
    public class NoNamespacePluginCommandTests
    {
        // Resolved with Path.Combine so it works on Windows and Linux alike.
        private static string GetTestPackageDir()
        {
#if DEBUG
            var configuration = "Debug";
#else
            var configuration = "Release";
#endif
            var frameworkName = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName ?? string.Empty;
            var targetFramework = frameworkName.Contains("Version=v")
                ? "net" + frameworkName.Split("Version=v")[1]
                : "net10.0";

            return Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "zTestCommandPackage", "bin", configuration, targetFramework));
        }

        [Fact]
        public void LoadPackageDescriptions_NoNamespaceCommand_FullTypeNameIsBareClassName()
        {
            var packages = new Crawler().LoadPackageDescriptions(GetTestPackageDir(), string.Empty);

            var package = packages.Values.FirstOrDefault(p => p.Commands.ContainsKey("NONS"));

            Assert.NotNull(package);
            Assert.Equal("NoNamespaceCommand", package!.Commands["NONS"].FullTypeName);
        }

        [Fact]
        public void CreateCommand_NoNamespaceCommand_LoadsFromPlugin()
        {
            var packages = new Crawler().LoadPackageDescriptions(GetTestPackageDir(), string.Empty);
            var package = packages.Values.First(p => p.Commands.ContainsKey("NONS"));
            var description = package.Commands["NONS"];

            var command = new CommandFactory().CreateCommand(description.FullTypeName, package.FullPath);

            Assert.Equal("NONS", command.Command);
        }
    }
}
