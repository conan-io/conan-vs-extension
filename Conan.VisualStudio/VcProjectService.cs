using System.IO;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio
{
    internal static class VcProjectService
    {
        public static ConanProject ExtractConanConfiguration(VCProject project) => new ConanProject
        {
            Path = project.ProjectDirectory,
            InstallPath = Path.Combine(project.ProjectDirectory, "conan"),
            Compiler = "Visual Studio",
            CompilerVersion = "15"
        };
    }
}
