using System.IO;

namespace Conan.VisualStudio.Tests
{
    internal static class FileSystemUtils
    {
        public static string CreateTempDirectory()
        {
            var path = Path.GetTempFileName();
            File.Delete(path);
            Directory.CreateDirectory(path);
            return path;
        }

        public static string CreateTempFile(string directory, string name)
        {
            var path = Path.Combine(directory, name);
            File.Create(path).Close();
            return path;
        }
    }
}
