using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core
{
    public static class ConanPathHelper
    {
        public static string DetermineConanPathFromEnvironment()
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? "";

            var executableExtensions = pathExt.Split(Path.PathSeparator);
            foreach (var directory in path.Split(Path.PathSeparator))
            foreach (var extension in executableExtensions)
            {
                var fileName = Path.ChangeExtension("conan", extension);
                var filePath = Path.Combine(directory, fileName);
                if (File.Exists(filePath))
                {
                    // to get the proper file name case:
                    return Directory.GetFiles(directory, fileName).Single();
                }
            }

            return null;
        }

        /// <summary>
        /// Searches for either conanfile.txt or conanfile.py in the directory or any of its' parent paths.
        /// </summary>
        /// <returns>Path to the nearest parent directory containing any type of conanfile.</returns>
        public static Task<string> GetNearestConanfilePath(string path) => Task.Run(() =>
        {
            while (path != null)
            {
                if (File.Exists(Path.Combine(path, "conanfile.py"))
                    || File.Exists(Path.Combine(path, "conanfile.txt")))
                {
                    break;
                }

                path = Directory.GetParent(path)?.FullName;
            }

            return path;
        });
    }
}
