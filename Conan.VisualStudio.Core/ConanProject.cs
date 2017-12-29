namespace Conan.VisualStudio.Core
{
    public class ConanProject
    {
        public string Path { get; set; }
        public string Architecture { get; set; }
        public string BuildType { get; set; }
        public string Compiler { get; set; }
        public string CompilerRuntime { get; set; }
        public string CompilerVersion { get; set; }
    }
}
