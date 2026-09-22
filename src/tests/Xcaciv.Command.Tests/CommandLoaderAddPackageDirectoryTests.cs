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
        public void AddPackageDirectory_PathIsAFile_ThrowsWithoutClaimingItDoesNotExist()
        {
            var filePath = Path.Combine(_restrictedDir, "not-a-directory.txt");
            File.WriteAllText(filePath, "x");
            var controller = new CommandController(new Crawler(), _restrictedDir);

            var ex = Assert.Throws<NoPackageDirectoryFoundException>(() => controller.AddPackageDirectory(filePath));

            Assert.Contains(filePath, ex.Message);
            Assert.Contains("not a directory", ex.Message);
        }

        [Fact]
        public void AddPackageDirectory_DirectoryOutsideRestrictedDirectory_ThrowsNamingBoth()
        {
            var controller = new CommandController(new Crawler(), _restrictedDir);

            var ex = Assert.Throws<NoPackageDirectoryFoundException>(() => controller.AddPackageDirectory(_outsideDir));

            Assert.Contains(_outsideDir, ex.Message);
            Assert.Contains(_restrictedDir, ex.Message);
        }

        /// <summary>
        /// docs/learn/getting-started-controller.md restricts the controller to a directory and
        /// then adds that same directory as the package directory. The restricted-path check
        /// used to compare the parent of the path, which rejected this.
        /// </summary>
        [Fact]
        public void AddPackageDirectory_RestrictedDirectoryItself_IsAdded()
        {
            var loader = new CommandLoader(new Crawler(), new VerifiedSourceDirectories());
            loader.SetRestrictedDirectory(_restrictedDir);

            loader.AddPackageDirectory(_restrictedDir);

            Assert.Contains(_restrictedDir, loader.Directories);
        }

        [Fact]
        public void AddPackageDirectory_SiblingOfRestrictedDirectory_IsRejected()
        {
            // "<restricted>2" shares the restricted directory's name as a prefix
            var prefixSibling = _restrictedDir + "2";
            Directory.CreateDirectory(prefixSibling);
            try
            {
                var loader = new CommandLoader(new Crawler(), new VerifiedSourceDirectories());
                loader.SetRestrictedDirectory(_restrictedDir);

                Assert.Throws<NoPackageDirectoryFoundException>(() => loader.AddPackageDirectory(prefixSibling));
            }
            finally
            {
                Directory.Delete(prefixSibling, true);
            }
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
