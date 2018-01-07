namespace Conan.VisualStudio.Core
{
    public class ConanConfiguration
    {
        public string Architecture { get; set; }
        public string BuildType { get; set; }
        public string CompilerToolset { get; set; }
        public string CompilerVersion { get; set; }

        public override string ToString()
        {
            return $"Architecture: {Architecture}, " +
                   $"build type: {BuildType}, " +
                   $"compiler toolset: {CompilerToolset}, " +
                   $"compiler version: {CompilerVersion}";
        }
    }
}
