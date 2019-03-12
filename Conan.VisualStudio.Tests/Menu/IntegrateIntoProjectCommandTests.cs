using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using Xunit;

namespace Conan.VisualStudio.Tests.Menu
{
    public class IntegrateIntoProjectCommandTests
    {
        [Fact]
        public async Task IntegrateIntoProjectCommandCalculatesAProjectRelativePathAsync()
        {
            var solutionDir = FileSystemUtils.CreateTempDirectory();
            FileSystemUtils.CreateTempFile(solutionDir, "conanfile.txt");

            var projectDir = Directory.CreateDirectory(Path.Combine(solutionDir, "Project")).FullName;
            var projectPath = Path.Combine(projectDir, "Project.vcxproj");

            var project = new Mock<VCProject>();
            project.Setup(p => p.ProjectDirectory).Returns(projectDir);
            project.Setup(p => p.ProjectFile).Returns(projectPath);

            var projectService = new Mock<IVcProjectService>();
            projectService.Setup(p => p.GetActiveProject()).Returns(project.Object);

            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(p => p.GetConanGenerator()).Returns(VisualStudio.Core.ConanGeneratorType.visual_studio_multi);

            var conanService = new Mock<IConanService>();

            var command = new IntegrateIntoProjectCommand(
                Mock.Of<IMenuCommandService>(),
                Mock.Of<IDialogService>(),
                projectService.Object,
                settingsService.Object,
                conanService.Object);
            await command.MenuItemCallbackAsync();

            conanService.Verify(p => p.IntegrateAsync(project.Object));
        }
    }
}
