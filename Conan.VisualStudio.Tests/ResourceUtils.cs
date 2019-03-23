using System;
using System.IO;
using System.Reflection;

namespace Conan.VisualStudio.Tests
{
    internal static class ResourceUtils
    {
        private static string GetResourcePath(string resourceName)
        {
            var assemblyDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            return Path.Combine(assemblyDirectory, "Resources", resourceName);
        }

        public static string ConanShim => GetResourcePath("conan-shim.cmd");
        public static string ConanShimError => GetResourcePath("conan-shim-error.cmd");
        public static string FakeProject => GetResourcePath("FakeProject.vcxproj");
    }
}
