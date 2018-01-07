using System.Linq;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Xunit;

namespace Conan.VisualStudio.Tests.Core
{
    public class ConanRunnerTests
    {
        [Fact]
        public async Task GeneratorShouldBeInvokedProperly()
        {
            var conan = new ConanRunner(ResourceUtils.ConanShim);
            var project = new ConanProject
            {
                Path = ".",
                InstallPath = "./conan",
                Configurations = 
                {
                    new ConanConfiguration
                    {
                        Architecture = "x86_64",
                        BuildType = "Debug",
                        CompilerToolset = "v141",
                        CompilerVersion = "15"
                    }
                }
            };
            using (var process = await conan.Install(project, project.Configurations.Single()))
            {
                Assert.Equal("install . -g visual_studio_multi " +
                             "--install-folder ./conan " +
                             "-s arch=x86_64 " +
                             "-s build_type=Debug " +
                             "-s compiler.toolset=v141 " +
                             "-s compiler.version=15 " +
                             "--build missing --update", process.StartInfo.Arguments);
            }
        }
    }
}
