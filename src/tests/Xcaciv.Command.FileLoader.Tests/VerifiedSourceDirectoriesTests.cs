using Xunit;
using Xcaciv.Command.FileLoader;
using System.IO.Abstractions.TestingHelpers;

namespace Xcaciv.Command.Tests.FileLoader
{
    public class VerifiedSourceDirectoriesTests
    {
        [Fact]
        public void VerifyRestrictedPath_ShouldReturnTrue_WhenFilePathIsWithinRestrictedPath()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/restricted/path");

            // Act
            var result = directories.VerifyRestrictedPath("/restricted/path/file.txt");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyRestrictedPath_ShouldReturnFalse_WhenFilePathIsNotWithinRestrictedPath()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/restricted/path");

            // Act
            var result = directories.VerifyRestrictedPath("/other/path/file.txt");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyFile_ShouldReturnTrue_WhenFileExistsWithinRestrictedPath()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile("/restricted/path/file.txt", new MockFileData("Test file"));
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/restricted/path");

            // Act
            var result = directories.VerifyFile("/restricted/path/file.txt");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyFile_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/restricted/path");

            // Act
            var result = directories.VerifyFile("/restricted/path/file.txt");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyDirectory_ShouldReturnTrue_WhenDirectoryExistsWithinRestrictedPath()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("/restricted/path/directory");
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/restricted/path");

            // Act
            var result = directories.VerifyDirectory("/restricted/path/directory");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyDirectory_ShouldReturnFalse_WhenDirectoryDoesNotExist()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/restricted/path");

            // Act
            var result = directories.VerifyDirectory("/restricted/path/directory");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AddDirectory_ShouldReturnTrue_WhenDirectoryIsValid()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("/valid/directory");
            var directories = new VerifiedSourceDirectories(fileSystem);
            directories.SetRestrictedDirectory("/valid");

            // Act
            var result = directories.AddDirectory("/valid/directory");

            // Assert
            Assert.True(result);
            Assert.Contains("/valid/directory", directories.Directories);
        }

        [Fact]
        public void AddDirectory_ShouldReturnFalse_WhenDirectoryIsInvalid()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            var directories = new VerifiedSourceDirectories(fileSystem);

            // Act
            var result = directories.AddDirectory("/invalid/directory");

            // Assert
            Assert.False(result);
            Assert.DoesNotContain("/invalid/directory", directories.Directories);
        }
    }
}
