using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.VCProjectEngine;
using Moq;
using Xunit;

namespace Conan.VisualStudio.Tests.Menu
{
    public class AddConanDependsTests
    {
        private readonly Mock<IDialogService> _dialogService = new Mock<IDialogService>();

        private static ConanProject NewTestProject(string path) => new ConanProject
        {
            Path = path,
            InstallPath = Path.Combine(path, "conan"),
            Configurations =
            {
                new ConanConfiguration
                {
                    CompilerVersion = "15"
                }
            }
        };

        private Task RunCommand(string conanPath, ConanProject project)
        {
            var vcProject = Mock.Of<VCProject>();
            Mock.Get(vcProject).Setup(x => x.Name).Returns("TestProject");

            var commandService = Mock.Of<IMenuCommandService>();
            _dialogService.Setup(x => x.ShowOkCancel(It.IsAny<string>())).Returns(true);

            var projectService = new Mock<IVcProjectService>();
            projectService.Setup(x => x.GetActiveProject()).Returns(vcProject);
            projectService.Setup(x => x.ExtractConanProject(It.IsAny<VCProject>())).ReturnsAsync(project);

            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(x => x.GetConanExecutablePath()).Returns(conanPath);

            var command = new AddConanDepends(
                commandService,
                _dialogService.Object,
                projectService.Object,
                settingsService.Object);
            return command.MenuItemCallback();
        }

        [Fact]
        public async Task AddConanDependsShowsAnErrorWindowIfConanReturnsExitCode()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            var project = NewTestProject(directory);

            await RunCommand(ResourceUtils.ConanShimError, project);

            const int exitCode = 10;
            var logFilePath = Path.Combine(directory, "conan", "conan.log");

            _dialogService.Verify(
                x => x.ShowPluginError(
                    $"Conan has returned exit code '{exitCode}' " +
                    $"while processing configuration '{project.Configurations.Single()}'. " +
                    $"Please check file '{logFilePath}' for details."));
            var logContent = File.ReadAllText(logFilePath);
            Assert.NotEmpty(logContent);
        }

        [Fact]
        public async Task AddConanDependsSuccedsIfLogDirectoryDoesNotExists()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            var project = NewTestProject(directory);

            await RunCommand(ResourceUtils.ConanShim, project);

            _dialogService.Verify(x => x.ShowPluginError(It.IsAny<string>()), Times.Never);
        }
    }
}
