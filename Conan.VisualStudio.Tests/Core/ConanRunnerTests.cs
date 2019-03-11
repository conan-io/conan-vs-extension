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
        [Fact(Skip = "FIXME: . disappears from path!")]
        public async Task GeneratorShouldBeInvokedProperlyAsync()
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
            using (var process = await conan.Install(project, project.Configurations.Single(), ConanGeneratorType.visual_studio_multi))
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

        [Fact(Skip = "FIXME: System.IO.FileLoadException : Could not load file or assembly 'Newtonsoft.Json, Version=12.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed' or one of its dependencies. The located assembly's manifest definition does not match the assembly ref")]
        public async Task SettingsFileShouldBeParsedProperlyAsync()
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

            using (var process = await conan.Install(project, project.Configurations.Single(), ConanGeneratorType.visual_studio_multi))
            {
                Assert.Equal("install -test", process.StartInfo.Arguments);
            }
        }
    }
}
