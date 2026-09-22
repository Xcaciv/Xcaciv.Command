using System;
using System.IO;
using Xcaciv.Command.FileLoader;
using Xcaciv.Command.Interface.Exceptions;
using Xunit;

namespace Xcaciv.Command.Tests
{
    /// <summary>
    /// Regression tests for how AddPackageDirectory reports a rejected directory. It used to
    /// discard the bool returned by VerifiedSourceDirectories.AddDirectory, so a bad directory
    /// only surfaced later as NoPluginsFoundException("No base package directory configured...")
    /// from LoadCommands, whatever the real cause was.
    /// </summary>
    public class CommandLoaderAddPackageDirectoryTests : IDisposable
    {
        private readonly string _restrictedDir;
        private readonly string _outsideDir;

        public CommandLoaderAddPackageDirectoryTests()
        {
            _restrictedDir = Path.Combine(Path.GetTempPath(), "xc-cl-restricted-" + Guid.NewGuid().ToString("N"));
            _outsideDir = Path.Combine(Path.GetTempPath(), "xc-cl-outside-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_restrictedDir);
            Directory.CreateDirectory(_outsideDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_restrictedDir)) Directory.Delete(_restrictedDir, true);
            if (Directory.Exists(_outsideDir)) Directory.Delete(_outsideDir, true);
        }

        [Fact]
        public void AddPackageDirectory_NonExistentDirectory_ThrowsNamingTheDirectory()
        {
            var nonExistent = Path.Combine(_restrictedDir, "does-not-exist");
            var controller = new CommandController(new Crawler(), _restrictedDir);

            var ex = Assert.Throws<NoPackageDirectoryFoundException>(() => controller.AddPackageDirectory(nonExistent));

            Assert.Contains(nonExistent, ex.Message);
            Assert.Contains("does not exist", ex.Message);
        }

        [Fact]
        public void AddPackageDirectory_DirectoryOutsideRestrictedDirectory_ThrowsNamingBoth()
        {
            var controller = new CommandController(new Crawler(), _restrictedDir);

            var ex = Assert.Throws<NoPackageDirectoryFoundException>(() => controller.AddPackageDirectory(_outsideDir));

            Assert.Contains(_outsideDir, ex.Message);
            Assert.Contains(_restrictedDir, ex.Message);
        }

        [Fact]
        public void AddPackageDirectory_ValidDirectory_IsAdded()
        {
            var validDir = Path.Combine(_restrictedDir, "plugins");
            Directory.CreateDirectory(validDir);
            var loader = new CommandLoader(new Crawler(), new VerifiedSourceDirectories());
            loader.SetRestrictedDirectory(_restrictedDir);

            loader.AddPackageDirectory(validDir);

            Assert.Contains(validDir, loader.Directories);
        }
    }
}
