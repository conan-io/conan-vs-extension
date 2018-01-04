using System;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Xunit;

namespace Conan.VisualStudio.Tests
{
    public class ConanPathHelperTests
    {
        [Fact]
        public void ConanPathIsDeterminedAutomatically()
        {
            var directory = CreateTempDirectory();
            const string extension = ".cmd";
            var conanShim = CreateTempFile(directory, "conan" + extension);

            Environment.SetEnvironmentVariable("PATH", directory);
            Environment.SetEnvironmentVariable("PATHEXT", extension);

            Assert.Equal(conanShim, ConanPathHelper.DetermineConanPathFromEnvironment());
        }

        [Fact]
        public void PathDeterminerRespectPathExtOrder()
        {
            var directory = CreateTempDirectory();
            var comShim = CreateTempFile(directory, "conan.com");
            CreateTempFile(directory, "conan.exe");
            var batShim = CreateTempFile(directory, "conan.bat");

            Environment.SetEnvironmentVariable("PATH", directory);

            Environment.SetEnvironmentVariable("PATHEXT", ".COM;.EXE;.BAT");
            Assert.Equal(comShim, ConanPathHelper.DetermineConanPathFromEnvironment());

            Environment.SetEnvironmentVariable("PATHEXT", ".BAT;.EXE;.COM");
            Assert.Equal(batShim, ConanPathHelper.DetermineConanPathFromEnvironment());
        }

        [Fact]
        public async Task GetNearestConanfilePathReturnsNullIfThereIsNoConanfile()
        {
            var dir = CreateTempDirectory();
            Assert.Null(await ConanPathHelper.GetNearestConanfilePath(dir));
        }

        [Fact]
        public async Task GetNearestConanfilePathReturnsCurrentPathIfValid()
        {
            var dir = CreateTempDirectory();
            var conanfile = CreateTempFile(dir, "conanfile.txt");
            Assert.Equal(dir, await ConanPathHelper.GetNearestConanfilePath(dir));

            File.Delete(conanfile);
            CreateTempFile(dir, "conanfile.py");
            Assert.Equal(dir, await ConanPathHelper.GetNearestConanfilePath(dir));
        }

        [Fact]
        public async Task GetNearestConanfilePathReturnsParentPathIfValid()
        {
            var dir = CreateTempDirectory();
            var subdir = Path.Combine(dir, "test");
            Directory.CreateDirectory(subdir);

            CreateTempFile(dir, "conanfile.txt");
            Assert.Equal(dir, await ConanPathHelper.GetNearestConanfilePath(subdir));
        }

        [Fact(Skip = "Manual test only; leaves traces at the disk root")]
        public async Task GetNearestConanfilePathWorksForDiskRoot()
        {
            var dir = CreateTempDirectory();
            var root = Path.GetPathRoot(dir);

            CreateTempFile(root, "conanfile.txt");
            Assert.Equal(root, await ConanPathHelper.GetNearestConanfilePath(dir));
            Assert.Equal(root, await ConanPathHelper.GetNearestConanfilePath(root));
        }

        private static string CreateTempDirectory()
        {
            var path = Path.GetTempFileName();
            File.Delete(path);
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CreateTempFile(string directory, string name)
        {
            var path = Path.Combine(directory, name);
            File.Create(path).Close();
            return path;
        }
    }
}
