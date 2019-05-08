using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Tests.Core
{
    [TestClass]
    public class ConanPathHelperTests
    {
        [DataTestMethod]
        [DataRow(@"C:\", @"C:\Program Files", "Program Files")]
        [DataRow(@"C:\Conan", @"C:\Conan\file.txt", "file.txt")]
        [DataRow(@"C:\Conan\", @"C:\Conan\file.txt", "file.txt")]
        [DataRow(@"C:\Conan", @"C:\Program Files", @"..\Program Files")]
        [DataRow(@"C:\Conan", @"D:\Program Files", @"D:\Program Files")]
        [DataRow(@"C:\Solution\Project", @"C:\Solution\conan\conanfile.props", @"..\conan\conanfile.props")]
        public void GetRelativePathTests(string basePath, string location, string expectedRelativePath) =>
            Assert.AreEqual(expectedRelativePath, ConanPathHelper.GetRelativePath(basePath, location));

        [TestMethod]
        public void ConanPathIsDeterminedAutomatically()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            const string extension = ".cmd";
            var conanShim = FileSystemUtils.CreateTempFile(directory, "conan" + extension);

            Environment.SetEnvironmentVariable("PATH", directory);
            Environment.SetEnvironmentVariable("PATHEXT", extension);

            Assert.AreEqual(conanShim, ConanPathHelper.DetermineConanPathFromEnvironment());
        }

        [TestMethod]
        public void PathDeterminerRespectPathExtOrder()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            var comShim = FileSystemUtils.CreateTempFile(directory, "conan.com");
            FileSystemUtils.CreateTempFile(directory, "conan.exe");
            var batShim = FileSystemUtils.CreateTempFile(directory, "conan.bat");

            Environment.SetEnvironmentVariable("PATH", directory);

            Environment.SetEnvironmentVariable("PATHEXT", ".COM;.EXE;.BAT");
            Assert.AreEqual(comShim, ConanPathHelper.DetermineConanPathFromEnvironment());

            Environment.SetEnvironmentVariable("PATHEXT", ".BAT;.EXE;.COM");
            Assert.AreEqual(batShim, ConanPathHelper.DetermineConanPathFromEnvironment());
        }

        [TestMethod]
        public async Task GetNearestConanfilePathReturnsNullIfThereIsNoConanfileAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            Assert.IsNull(await ConanPathHelper.GetNearestConanfilePathAsync(dir));
        }

        [TestMethod]
        public async Task GetNearestConanfilePathReturnsCurrentPathIfValidAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            var conanfile = FileSystemUtils.CreateTempFile(dir, "conanfile.txt");
            Assert.AreEqual(dir, await ConanPathHelper.GetNearestConanfilePathAsync(dir));

            File.Delete(conanfile);
            FileSystemUtils.CreateTempFile(dir, "conanfile.py");
            Assert.AreEqual(dir, await ConanPathHelper.GetNearestConanfilePathAsync(dir));
        }

        [TestMethod]
        public async Task GetNearestConanfilePathReturnsParentPathIfValidAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            var subdir = Path.Combine(dir, "test");
            Directory.CreateDirectory(subdir);

            FileSystemUtils.CreateTempFile(dir, "conanfile.txt");
            Assert.AreEqual(dir, await ConanPathHelper.GetNearestConanfilePathAsync(subdir));
        }
        [Ignore("Manual test only; leaves traces at the disk root")]
        [TestMethod]
        public async Task GetNearestConanfilePathWorksForDiskRootAsync()
        {
            var dir = FileSystemUtils.CreateTempDirectory();
            var root = Path.GetPathRoot(dir);

            FileSystemUtils.CreateTempFile(root, "conanfile.txt");
            Assert.AreEqual(root, await ConanPathHelper.GetNearestConanfilePathAsync(dir));
            Assert.AreEqual(root, await ConanPathHelper.GetNearestConanfilePathAsync(root));
        }
    }
}
