using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Xunit;

namespace Conan.VisualStudio.Tests
{
    public class ConanTests
    {
        [Fact]
        public async Task GeneratorShouldBeInvokedProperly()
        {
            var assemblyDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var conanPath = Path.Combine(assemblyDirectory, "conan-shim.cmd");
            var conan = new Core.Conan(conanPath);
            var project = new ConanProject
            {
                Path = ".",
                Compiler = "Visual Studio",
                CompilerVersion = "15",
                Architecture = "x86_64",
                BuildType = "Debug",
                CompilerRuntime = "MD"
            };
            using (var process = await conan.Install(project))
            {
                var arguments = process.StartInfo.Arguments;
                Assert.Equal("install . -g visual_studio_multi " +
                             "-s arch=x86_64 " +
                             "-s build_type=Debug " +
                             "-s compiler=\"Visual Studio\" " +
                             "-s compiler.runtime=MD " +
                             "-s compiler.version=15 " +
                             "--build missing --update", arguments);
            }
        }
    }
}
