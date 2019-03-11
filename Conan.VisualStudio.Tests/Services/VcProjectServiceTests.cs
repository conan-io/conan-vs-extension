using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using Xunit;

namespace Conan.VisualStudio.Tests.Services
{
    public class VcProjectServiceTests
    {
        private readonly IVcProjectService _service = new VcProjectService();

        [Fact]
        public void GetArchitrectureSupportsTheNecessaryArchitectures()
        {
            Assert.Equal("x86", VcProjectService.GetArchitecture("Win32"));
            Assert.Equal("x86_64", VcProjectService.GetArchitecture("x64"));
        }

        [Fact]
        public void GetBuildTypeReturnsItsArgument()
        {
            var configurationName = Guid.NewGuid().ToString();
            Assert.Equal(configurationName, VcProjectService.GetBuildType(configurationName));
        }

        [Fact]
        public async Task ExtractConanProjectExtractsThePathsProperlyAsync()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            FileSystemUtils.CreateTempFile(directory, "conanfile.txt");
            var installPath = Path.Combine(directory, ".conan");

            var vcProject = MockVcProject(directory);

            var project = await _service.ExtractConanProjectAsync(vcProject, null);
            Assert.Equal(directory, project.Path);
            Assert.Equal(installPath, project.InstallPath);
        }

        [Fact]
        public async Task ExtractConanConfigurationExtractsAllTheDataAsync()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            FileSystemUtils.CreateTempFile(directory, "conanfile.txt");

            var vcConfiguration = MockVcConfiguration("Win32", "Debug", "v141");
            var vcProject = MockVcProject(directory, vcConfiguration);

            var project = await _service.ExtractConanProjectAsync(vcProject, null);
            var configuration = project.Configurations.Single();

            Assert.Equal("x86", configuration.Architecture);
            Assert.Equal("Debug", configuration.BuildType);
            Assert.Equal("v141", configuration.CompilerToolset);
            Assert.Equal("15", configuration.CompilerVersion);
        }

        [Theory]
        [InlineData(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
</Project>")]
        [InlineData(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Import Project=""foo\other.props"" />
</Project>")]
        [InlineData(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Import Project=""foo\bar.props"" />
</Project>")]
        public async Task IntegrateAddsImportIfNecessaryAsync(string projectText)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, projectText);

            const string propFilePath = @"foo\bar.props";
            await new VcProjectService().AddPropsImportAsync(path, propFilePath);

            var document = XDocument.Load(path);
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            var imports = document.Root.Descendants(ns + "Import");
            Assert.Single(imports, import => import.Attribute("Project").Value == propFilePath);
        }

        private VCProject MockVcProject(string directory, params VCConfiguration[] configurations)
        {
            var project = new Mock<VCProject>();
            project.Setup(p => p.ProjectDirectory).Returns(directory);
            project.Setup(p => p.Configurations).Returns(configurations);
            return project.Object;
        }

        private VCConfiguration MockVcConfiguration(string platformName, string configurationName, string platformToolset)
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

            return configuration.Object;
        }
    }
}
