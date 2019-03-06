using System;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Xunit;

namespace Conan.VisualStudio.Tests.Core
{
    public class ConanPathHelperTests
    {
        [Theory]
        [InlineData(@"C:\", @"C:\Program Files", "Program Files")]
        [InlineData(@"C:\Conan", @"C:\Conan\file.txt", "file.txt")]
        [InlineData(@"C:\Conan\", @"C:\Conan\file.txt", "file.txt")]
        [InlineData(@"C:\Conan", @"C:\Program Files", @"..\Program Files")]
        [InlineData(@"C:\Conan", @"D:\Program Files", @"D:\Program Files")]
        [InlineData(@"C:\Solution\Project", @"C:\Solution\conan\conanfile.props", @"..\conan\conanfile.props")]
        public void GetRelativePathTests(string basePath, string location, string expectedRelativePath) =>
            Assert.Equal(expectedRelativePath, ConanPathHelper.GetRelativePath(basePath, location));

        [Fact]
        public void ConanPathIsDeterminedAutomatically()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            const string extension = ".cmd";
            var conanShim = FileSystemUtils.CreateTempFile(directory, "conan" + extension);

            Environment.SetEnvironmentVariable("PATH", directory);
            Environment.SetEnvironmentVariable("PATHEXT", extension);

            Assert.Equal(conanShim, ConanPathHelper.DetermineConanPathFromEnvironment());
        }

        [Fact]
        public void PathDeterminerRespectPathExtOrder()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            var comShim = FileSystemUtils.CreateTempFile(directory, "conan.com");
            FileSystemUtils.CreateTempFile(directory, "conan.exe");
            var batShim = FileSystemUtils.CreateTempFile(directory, "conan.bat");

            Environment.SetEnvironmentVariable("PATH", directory);

            Environment.SetEnvironmentVariable("PATHEXT", ".COM;.EXE;.BAT");
            Assert.Equal(comShim, ConanPathHelper.DetermineConanPathFromEnvironment());

            Environment.SetEnvironmentVariable("PATHEXT", ".BAT;.EXE;.COM");
            Assert.Equal(batShim, ConanPathHelper.DetermineConanPathFromEnvironment());
        }

        [Fact]
        public async Task GetNearestConanfilePathReturnsNullIfThereIsNoConanfileAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            Assert.Null(await ConanPathHelper.GetNearestConanfilePath(dir));
        }

        [Fact]
        public async Task GetNearestConanfilePathReturnsCurrentPathIfValidAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            var conanfile = FileSystemUtils.CreateTempFile(dir, "conanfile.txt");
            Assert.Equal(dir, await ConanPathHelper.GetNearestConanfilePath(dir));

            File.Delete(conanfile);
            FileSystemUtils.CreateTempFile(dir, "conanfile.py");
            Assert.Equal(dir, await ConanPathHelper.GetNearestConanfilePath(dir));
        }

        [Fact]
        public async Task GetNearestConanfilePathReturnsParentPathIfValidAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            var subdir = Path.Combine(dir, "test");
            Directory.CreateDirectory(subdir);

            FileSystemUtils.CreateTempFile(dir, "conanfile.txt");
            Assert.Equal(dir, await ConanPathHelper.GetNearestConanfilePath(subdir));
        }

        [Fact(Skip = "Manual test only; leaves traces at the disk root")]
        public async Task GetNearestConanfilePathWorksForDiskRootAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            var root = Path.GetPathRoot(dir);

            FileSystemUtils.CreateTempFile(root, "conanfile.txt");
            Assert.Equal(root, await ConanPathHelper.GetNearestConanfilePath(dir));
            Assert.Equal(root, await ConanPathHelper.GetNearestConanfilePath(root));
        }
    }
}
