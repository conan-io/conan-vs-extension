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
