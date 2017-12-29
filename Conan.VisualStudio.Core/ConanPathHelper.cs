using System;
using System.IO;
using System.Linq;

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
    }
}
