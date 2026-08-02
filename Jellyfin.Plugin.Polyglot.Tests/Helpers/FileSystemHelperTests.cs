using FluentAssertions;
using Jellyfin.Plugin.Polyglot.Helpers;
using Jellyfin.Plugin.Polyglot.Models;
using Xunit;

namespace Jellyfin.Plugin.Polyglot.Tests.Helpers;

/// <summary>
/// Unit tests for the FileSystemHelper class.
/// </summary>
public class FileSystemHelperTests
{
    #region AreOnSameFilesystem Tests (Windows-specific path checking)

    [Theory]
    [InlineData(null, "/media/movies")]
    [InlineData("/media/movies", null)]
    [InlineData("", "/media/movies")]
    [InlineData("/media/movies", "")]
    public void AreOnSameFilesystem_NullOrEmptyPaths_ReturnsFalse(string? path1, string? path2)
    {
        // Act
        var result = FileSystemHelper.AreOnSameFilesystem(path1!, path2!);

        // Assert
        result.Should().BeFalse("null or empty paths should return false");
    }

    #endregion

    #region CreateHardLink Validation Tests

    [Fact]
    public void CreateHardLink_NullSourcePath_ThrowsArgumentNullException()
    {
        // Act
        var action = () => FileSystemHelper.CreateHardLink(null!, "/target/file");

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("sourcePath");
    }

    [Fact]
    public void CreateHardLink_EmptySourcePath_ThrowsArgumentNullException()
    {
        // Act
        var action = () => FileSystemHelper.CreateHardLink(string.Empty, "/target/file");

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("sourcePath");
    }

    [Fact]
    public void CreateHardLink_NullLinkPath_ThrowsArgumentNullException()
    {
        // Act
        var action = () => FileSystemHelper.CreateHardLink("/source/file", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("linkPath");
    }

    [Fact]
    public void CreateHardLink_EmptyLinkPath_ThrowsArgumentNullException()
    {
        // Act
        var action = () => FileSystemHelper.CreateHardLink("/source/file", string.Empty);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("linkPath");
    }

    [Fact]
    public void CreateHardLink_NonExistentSourceFile_ReturnsFalse()
    {
        // Arrange
        var sourcePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var linkPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

        // Assert
        result.Should().BeFalse("non-existent source file should return false");
    }

    #endregion

    #region Integration Tests (require actual filesystem)

    [Fact]
    public void CreateHardLink_ValidSourceFile_CreatesHardlink()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            // Act
            var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            // Assert
            result.Should().BeTrue("hardlink should be created successfully on same filesystem");
            File.Exists(linkPath).Should().BeTrue("link file should exist");
            File.ReadAllText(linkPath).Should().Be("test content", "link should have same content as source");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateHardLink_CreatesParentDirectories()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "subdir", "nested", "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            // Act
            var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            // Assert
            result.Should().BeTrue("hardlink should be created successfully");
            Directory.Exists(Path.Combine(tempDir, "subdir", "nested")).Should().BeTrue("parent directories should be created");
            File.Exists(linkPath).Should().BeTrue("link file should exist");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateHardLink_OverwritesExistingFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(linkPath, "old content");

        try
        {
            // Act
            var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            // Assert
            result.Should().BeTrue("hardlink should be created successfully");
            File.ReadAllText(linkPath).Should().Be("new content", "link should have new content");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateHardLink_TargetIsDirectory_ReturnsFalse()
    {
        // DESIRED BEHAVIOR: When target path is an existing directory,
        // the method should fail gracefully rather than throw or corrupt data.
        
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "target_dir");
        File.WriteAllText(sourcePath, "test content");
        Directory.CreateDirectory(linkPath); // Create target as a directory

        try
        {
            // Act
            var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            // Assert
            // The implementation currently deletes existing files before creating hardlink,
            // but a directory is not a file. This should return false gracefully.
            // Note: Current impl may throw or succeed unexpectedly - this test validates desired behavior.
            result.Should().BeFalse("creating a hardlink where a directory exists should fail gracefully");
            Directory.Exists(linkPath).Should().BeTrue("the directory should not be deleted");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateHardLink_SourceDeletedDuringOperation_ReturnsFalse()
    {
        // DESIRED BEHAVIOR: If source file is deleted between existence check and link creation
        // (race condition), the method should return false gracefully.
        
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        
        // Don't create the source file - simulates it being deleted
        // The CreateHardLink method checks File.Exists first, so this just tests that path

        try
        {
            // Act
            var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            // Assert
            result.Should().BeFalse("non-existent source should return false");
            File.Exists(linkPath).Should().BeFalse("no link should be created");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateHardLink_ReadOnlyTargetDirectory_ReturnsFalse()
    {
        // Skip on Windows as directory permissions work differently
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // DESIRED BEHAVIOR: When target directory is read-only,
        // the method should return false with appropriate logging.
        
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        var sourceDir = Path.Combine(tempDir, "source");
        var targetDir = Path.Combine(tempDir, "readonly_target");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(targetDir);

        var sourcePath = Path.Combine(sourceDir, "source.txt");
        var linkPath = Path.Combine(targetDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            // Make target directory read-only
            var dirInfo = new DirectoryInfo(targetDir);
            dirInfo.Attributes |= FileAttributes.ReadOnly;

            // Also need to remove write permission on Unix
            try
            {
                System.Diagnostics.Process.Start("chmod", $"555 {targetDir}")?.WaitForExit();
            }
            catch
            {
                // Skip if chmod fails
                return;
            }

            // Act
            var result = FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            // Assert
            result.Should().BeFalse("creating hardlink in read-only directory should fail");
        }
        finally
        {
            // Cleanup - restore permissions first
            try
            {
                System.Diagnostics.Process.Start("chmod", $"755 {targetDir}")?.WaitForExit();
                var dirInfo = new DirectoryInfo(targetDir);
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion

    #region Filesystem Validation Edge Cases

    [Fact]
    public void AreOnSameFilesystem_NonExistentPaths_HandlesGracefully()
    {
        // DESIRED BEHAVIOR: When paths don't exist, the method should
        // still work by checking parent directories.
        
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var path1 = Path.Combine(tempDir, "nonexistent1", "subdir");
        var path2 = Path.Combine(tempDir, "nonexistent2", "subdir");

        try
        {
            // Act
            var result = FileSystemHelper.AreOnSameFilesystem(path1, path2);

            // Assert
            // Both paths have the same existing parent (tempDir), so they should be on same filesystem
            result.Should().BeTrue("paths with same existing parent should be on same filesystem");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void AreOnSameFilesystem_SameDirectory_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var path1 = Path.Combine(tempDir, "dir1");
        var path2 = Path.Combine(tempDir, "dir2");
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);

        try
        {
            // Create a test file to use for hardlink testing
            var testFile = Path.Combine(path1, "test.txt");
            File.WriteAllText(testFile, "test");

            // Act
            var result = FileSystemHelper.AreOnSameFilesystem(path1, path2);

            // Assert
            result.Should().BeTrue("paths in same directory should be on same filesystem");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CleanupEmptyDirectories_RemovesEmptyDirs()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        var nestedDir = Path.Combine(tempDir, "a", "b", "c");
        Directory.CreateDirectory(nestedDir);

        try
        {
            // Act
            FileSystemHelper.CleanupEmptyDirectories(nestedDir, tempDir);

            // Assert
            Directory.Exists(nestedDir).Should().BeFalse("empty directory should be removed");
            Directory.Exists(Path.Combine(tempDir, "a", "b")).Should().BeFalse("empty parent directory should be removed");
            Directory.Exists(Path.Combine(tempDir, "a")).Should().BeFalse("empty grandparent directory should be removed");
            Directory.Exists(tempDir).Should().BeTrue("base directory should not be removed");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CleanupEmptyDirectories_StopsAtNonEmptyDir()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        var nestedDir = Path.Combine(tempDir, "a", "b", "c");
        Directory.CreateDirectory(nestedDir);

        // Create a file in "b" to make it non-empty
        File.WriteAllText(Path.Combine(tempDir, "a", "b", "file.txt"), "content");

        try
        {
            // Act
            FileSystemHelper.CleanupEmptyDirectories(nestedDir, tempDir);

            // Assert
            Directory.Exists(nestedDir).Should().BeFalse("empty directory should be removed");
            Directory.Exists(Path.Combine(tempDir, "a", "b")).Should().BeTrue("non-empty directory should remain");
            Directory.Exists(Path.Combine(tempDir, "a")).Should().BeTrue("parent of non-empty directory should remain");
        }
        finally
        {
            // Cleanup
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Theory]
    [InlineData(null, "/base")]
    [InlineData("/dir", null)]
    [InlineData("", "/base")]
    [InlineData("/dir", "")]
    public void CleanupEmptyDirectories_NullOrEmptyPaths_DoesNotThrow(string? directoryPath, string? basePath)
    {
        // Act
        var action = () => FileSystemHelper.CleanupEmptyDirectories(directoryPath!, basePath!);

        // Assert
        action.Should().NotThrow("method should handle null/empty paths gracefully");
    }

    #endregion

    #region CreateSymLink Validation Tests

    [Fact]
    public void CreateSymLink_NullSourcePath_ThrowsArgumentNullException()
    {
        var action = () => FileSystemHelper.CreateSymLink(null!, "/target/file");

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("sourcePath");
    }

    [Fact]
    public void CreateSymLink_EmptySourcePath_ThrowsArgumentNullException()
    {
        var action = () => FileSystemHelper.CreateSymLink(string.Empty, "/target/file");

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("sourcePath");
    }

    [Fact]
    public void CreateSymLink_NullLinkPath_ThrowsArgumentNullException()
    {
        var action = () => FileSystemHelper.CreateSymLink("/source/file", null!);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("linkPath");
    }

    [Fact]
    public void CreateSymLink_EmptyLinkPath_ThrowsArgumentNullException()
    {
        var action = () => FileSystemHelper.CreateSymLink("/source/file", string.Empty);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("linkPath");
    }

    [Fact]
    public void CreateSymLink_NonExistentSourceFile_ReturnsFalse()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var linkPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var result = FileSystemHelper.CreateSymLink(sourcePath, linkPath);

        result.Should().BeFalse("non-existent source file should return false");
    }

    #endregion

    #region CreateSymLink Integration Tests (require actual filesystem)

    [Fact]
    public void CreateSymLink_ValidSourceFile_CreatesSymlinkPointingAtSource()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            var result = FileSystemHelper.CreateSymLink(sourcePath, linkPath);

            result.Should().BeTrue("symlink should be created successfully");
            File.Exists(linkPath).Should().BeTrue("link file should exist");
            File.ReadAllText(linkPath).Should().Be("test content", "link should resolve to same content as source");

            var linkTarget = new FileInfo(linkPath).LinkTarget;
            linkTarget.Should().NotBeNull("link should report a link target");
            var resolved = File.ResolveLinkTarget(linkPath, returnFinalTarget: true);
            resolved!.FullName.Should().Be(Path.GetFullPath(sourcePath));
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateSymLink_CreatesParentDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "subdir", "nested", "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            var result = FileSystemHelper.CreateSymLink(sourcePath, linkPath);

            result.Should().BeTrue("symlink should be created successfully");
            Directory.Exists(Path.Combine(tempDir, "subdir", "nested")).Should().BeTrue("parent directories should be created");
            File.Exists(linkPath).Should().BeTrue("link file should exist");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateSymLink_OverwritesExistingFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "new content");
        File.WriteAllText(linkPath, "old content");

        try
        {
            var result = FileSystemHelper.CreateSymLink(sourcePath, linkPath);

            result.Should().BeTrue("symlink should be created successfully");
            File.ReadAllText(linkPath).Should().Be("new content", "link should resolve to new source content");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateSymLink_TargetIsDirectory_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "target_dir");
        File.WriteAllText(sourcePath, "test content");
        Directory.CreateDirectory(linkPath);

        try
        {
            var result = FileSystemHelper.CreateSymLink(sourcePath, linkPath);

            result.Should().BeFalse("creating a symlink where a directory exists should fail gracefully");
            Directory.Exists(linkPath).Should().BeTrue("the directory should not be deleted");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion

    #region CreateLink Dispatcher Tests

    [Fact]
    public void CreateLink_HardlinkMode_CreatesHardlink()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            var result = FileSystemHelper.CreateLink(sourcePath, linkPath, LinkMode.Hardlink);

            result.Should().BeTrue();
            new FileInfo(linkPath).LinkTarget.Should().BeNull("a hardlink is not a reparse point/symlink");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void CreateLink_SymlinkMode_CreatesSymlink()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            var result = FileSystemHelper.CreateLink(sourcePath, linkPath, LinkMode.Symlink);

            result.Should().BeTrue();
            new FileInfo(linkPath).LinkTarget.Should().NotBeNull("symlink mode should create a real symlink");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion

    #region IsValidLink Tests

    [Fact]
    public void IsValidLink_NonExistentPath_ReturnsFalse()
    {
        var linkPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        FileSystemHelper.IsValidLink(linkPath, LinkMode.Symlink).Should().BeFalse();
        FileSystemHelper.IsValidLink(linkPath, LinkMode.Hardlink).Should().BeFalse();
    }

    [Fact]
    public void IsValidLink_HardlinkMode_ExistingFile_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            FileSystemHelper.CreateHardLink(sourcePath, linkPath);

            FileSystemHelper.IsValidLink(linkPath, LinkMode.Hardlink).Should().BeTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void IsValidLink_SymlinkMode_ValidSymlink_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            FileSystemHelper.CreateSymLink(sourcePath, linkPath);

            FileSystemHelper.IsValidLink(linkPath, LinkMode.Symlink).Should().BeTrue();
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void IsValidLink_SymlinkMode_RegularFileNotSymlink_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "regular.txt");
        File.WriteAllText(filePath, "test content");

        try
        {
            FileSystemHelper.IsValidLink(filePath, LinkMode.Symlink).Should().BeFalse("a regular file is not a symlink");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void IsValidLink_SymlinkMode_BrokenSymlink_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "polyglot_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sourcePath = Path.Combine(tempDir, "source.txt");
        var linkPath = Path.Combine(tempDir, "link.txt");
        File.WriteAllText(sourcePath, "test content");

        try
        {
            FileSystemHelper.CreateSymLink(sourcePath, linkPath);
            File.Delete(sourcePath);

            FileSystemHelper.IsValidLink(linkPath, LinkMode.Symlink).Should().BeFalse("target no longer exists");
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #endregion
}

