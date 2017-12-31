using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Xunit;

namespace Conan.VisualStudio.Tests
{
    public class ConanRunnerTests
    {
        [Fact]
        public async Task GeneratorShouldBeInvokedProperly()
        {
            var assemblyDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var conanPath = Path.Combine(assemblyDirectory, "conan-shim.cmd");
            var conan = new ConanRunner(conanPath);
            var project = new ConanProject
            {
                Path = ".",
                InstallPath = "./conan",
                Compiler = "Visual Studio",
                CompilerVersion = "15"
            };
            using (var process = await conan.Install(project))
            {
                Assert.Equal("install . -g visual_studio_multi " +
                             "--install-folder ./conan " +
                             "-s compiler.version=15 " +
                             "--build missing --update", process.StartInfo.Arguments);
            }
        }
    }
}
