using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Services;
using Xunit;

namespace Conan.VisualStudio.Tests.Core
{
    public class ConanRunnerTests
    {
        [Fact]
        public async Task GeneratorShouldBeInvokedProperly()
        {
            var conan = new ConanRunner(null, ResourceUtils.ConanShim);
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

        [Fact]
        public async Task SettingsFileShouldBeParsedProperly()
        {
            var project = new ConanProject
            {
                Path = ResourceUtils.FakeProject,
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

            var settingsService = new VisualStudioSettingsService(null);
            var conanSettings = settingsService.LoadSettingFile(project);

            var conan = new ConanRunner(conanSettings, ResourceUtils.ConanShim);

            using (var process = await conan.Install(project, project.Configurations.Single()))
            {
                Assert.Equal("install -test", process.StartInfo.Arguments);
            }
        }
    }
}
