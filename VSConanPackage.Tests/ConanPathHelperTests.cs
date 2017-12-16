using System;
using System.IO;
using Xunit;

namespace VSConanPackage.Tests
{
    public class ConanPathHelperTests
    {
        [Fact]
        public void ConanPathGetsDeterminedAutomatically()
        {
            var directory = CreateTempDirectory();
            const string extension = ".cmd";
            var conanShim = CreateTempFile(directory, "conan" + extension);

            Environment.SetEnvironmentVariable("PATH", directory);
            Environment.SetEnvironmentVariable("PATHEXT", extension);

            Assert.Equal(conanShim, ConanPathHelper.DetermineConanPathFromEnvironment());
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
