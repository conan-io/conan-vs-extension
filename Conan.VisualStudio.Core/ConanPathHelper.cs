using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core
{
    public static class ConanPathHelper
    {
        /// <summary>
        /// Returns a path to <paramref name="location"/> relative to <paramref name="baseDirectory" />. If the paths
        /// aren't related, returns an absolute path to the <paramref name="location"/>.
        /// </summary>
        public static string GetRelativePath(string baseDirectory, string location)
        {
            if (!Path.GetFullPath(baseDirectory).EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                baseDirectory = Path.GetFullPath(baseDirectory) + Path.DirectorySeparatorChar;
            }

            var baseUri = new Uri(baseDirectory);
            var locationUri = new Uri(location);
            var relativeUri = baseUri.MakeRelativeUri(locationUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string DetermineConanPathFromEnvironment()
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? "";

            var executableExtensions = pathExt.Split(Path.PathSeparator);
            foreach (var directory in path.Split(Path.PathSeparator))
            {
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
            }

            return null;
        }

        /// <summary>
        /// Searches for either conanfile.txt or conanfile.py in the directory or any of its' parent paths.
        /// </summary>
        /// <returns>Path to the nearest parent directory containing any type of conanfile.</returns>
        public static string GetNearestConanfilePath(string path)
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
        }

        public static async Task<string> GetNearestConanfilePathAsync(string path) => await Task.Run(() =>
        {
            return GetNearestConanfilePath(path);
        });

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }
    }
}
