using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;

namespace Conan.VisualStudio.Tests.Services
{
    public class VcProjectServiceTests
    {
        private readonly IVcProjectService _service = new VcProjectService();

        [TestMethod]
        public void GetArchitrectureSupportsTheNecessaryArchitectures()
        {
            Assert.AreEqual("x86", VcProjectService.GetArchitecture("Win32"));
            Assert.AreEqual("x86_64", VcProjectService.GetArchitecture("x64"));
        }

        [TestMethod]
        public void GetBuildTypeReturnsItsArgument()
        {
            var configurationName = Guid.NewGuid().ToString();
            Assert.AreEqual(configurationName, VcProjectService.GetBuildType(configurationName));
        }

        [TestMethod]
        public async Task ExtractConanProjectExtractsThePathsProperlyAsync()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            FileSystemUtils.CreateTempFile(directory, "conanfile.txt");
            var installPath = Path.Combine(directory, ".conan");

            var vcProject = MockVcProject(directory);

            var project = await _service.ExtractConanProjectAsync(vcProject, null);
            Assert.AreEqual(directory, project.Path);
        }

        [TestMethod]
        public async Task ExtractConanConfigurationExtractsAllTheDataAsync()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            FileSystemUtils.CreateTempFile(directory, "conanfile.txt");

            var vcConfiguration = MockVcConfiguration("Win32", "Debug", "v141");
            var vcProject = MockVcProject(directory, vcConfiguration.Object);
            var vcToolsCollection = new Mock<IVCCollection>();
            var vcTools = new Mock<VCCLCompilerTool>();

            vcConfiguration.Setup(c => c.project).Returns(vcProject);
            vcConfiguration.Setup(c => c.Tools).Returns(vcToolsCollection.Object);
            vcToolsCollection.Setup(c => c.Item("VCCLCompilerTool")).Returns(vcTools.Object);

            vcTools.Setup(c => c.RuntimeLibrary).Returns(runtimeLibraryOption.rtMultiThreadedDLL);

            var project = await _service.ExtractConanProjectAsync(vcProject, null);
            var configuration = project.Configurations.Single();

            Assert.AreEqual("x86", configuration.Architecture);
            Assert.AreEqual("Debug", configuration.BuildType);
            Assert.AreEqual("v141", configuration.CompilerToolset);
            Assert.AreEqual("15", configuration.CompilerVersion);
            Assert.AreEqual("MD", configuration.RuntimeLibrary);
        }


        private VCProject MockVcProject(string directory, params VCConfiguration[] configurations)
        {
            var project = new Mock<VCProject>();
            project.Setup(p => p.ProjectDirectory).Returns(directory);
            project.Setup(p => p.Configurations).Returns(configurations);
            return project.Object;
        }

        private Mock<VCConfiguration> MockVcConfiguration(string platformName, string configurationName, string platformToolset)
        {
            var configuration = new Mock<VCConfiguration>();
            configuration.Setup(c => c.ConfigurationName).Returns(configurationName);

            object MockPlatform()
            {
                dynamic platform = new ExpandoObject();
                platform.Name = platformName;
                return platform;
            }

            configuration.Setup(c => c.Platform).Returns(MockPlatform());

            var generalSettings = new Mock<IVCRulePropertyStorage>();
            generalSettings.Setup(s => s.GetEvaluatedPropertyValue("PlatformToolset")).Returns(platformToolset);

            var rulesCollection = new Mock<IVCCollection>();
            rulesCollection.Setup(c => c.Item("ConfigurationGeneral")).Returns(generalSettings.Object);
            configuration.Setup(c => c.Rules).Returns(rulesCollection.Object);

            return configuration;
        }
    }
}
