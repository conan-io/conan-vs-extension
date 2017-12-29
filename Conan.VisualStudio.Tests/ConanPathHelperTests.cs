using System;
using System.IO;
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
