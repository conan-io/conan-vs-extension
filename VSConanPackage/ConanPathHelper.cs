using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VSConanPackage
{
    internal class ConanPathHelper
    {
        public static string DetermineConanPathFromEnvironment()
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? "";

            var pathComparer = StringComparer.InvariantCultureIgnoreCase;
            var executableExtensions = new HashSet<string>(pathExt.Split(Path.PathSeparator), pathComparer);
            foreach (var item in path.Split(Path.PathSeparator))
            {
                var files = Directory.GetFiles(item);
                var executables = files.Where(x => executableExtensions.Contains(Path.GetExtension(x)));
                var conanExecutable = executables
                    .FirstOrDefault(x => pathComparer.Equals(Path.GetFileNameWithoutExtension(x), "conan"));
                if (conanExecutable != null)
                {
                    return conanExecutable;
                }
            }

            return null;
        }
    }
}
