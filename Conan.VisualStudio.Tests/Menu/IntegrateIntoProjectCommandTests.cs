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
        public async Task IntegrateIntoProjectCommandCalculatesAProjectRelativePath()
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

            var command = new IntegrateIntoProjectCommand(
                Mock.Of<IMenuCommandService>(),
                Mock.Of<IDialogService>(),
                projectService.Object);
            await command.MenuItemCallback();

            projectService.Verify(p => p.AddPropsImport(projectPath, @"..\conan\conanbuildinfo_multi.props"));
        }
    }
}
