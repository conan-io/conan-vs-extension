using System;
using System.ComponentModel.Design;
using System.IO;
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

        private Task RunCommand(string conanPath, ConanProject project)
        {
            var vcProject = Mock.Of<VCProject>();
            Mock.Get(vcProject).Setup(x => x.Name).Returns("TestProject");

            var commandService = Mock.Of<IMenuCommandService>();
            _dialogService.Setup(x => x.ShowOkCancel(It.IsAny<string>())).Returns(true);

            var projectService = new Mock<IVcProjectService>();
            projectService.Setup(x => x.GetActiveProject()).Returns(vcProject);
            projectService.Setup(x => x.ExtractConanConfiguration(It.IsAny<VCProject>())).ReturnsAsync(project);

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
            var project = new ConanProject
            {
                Path = directory,
                InstallPath = directory,
                CompilerVersion = "15"
            };

            await RunCommand(ResourceUtils.ConanShimError, project);

            const int exitCode = 10;
            var logFilePath = Path.Combine(directory, "conan.log");

            _dialogService.Verify(
                x => x.ShowPluginError(
                    $"Conan has returned exit code {exitCode}. Please check file '{logFilePath}' for details."));
            var logContent = File.ReadAllText(Path.Combine(directory, "conan.log"));
            Assert.Equal($"conan-shim-error{Environment.NewLine}", logContent);
        }

        [Fact]
        public async Task AddConanDependsSuccedsIfLogDirectoryDoesNotExists()
        {
            var directory = FileSystemUtils.CreateTempDirectory();

            var project = new ConanProject
            {
                Path = directory,
                InstallPath = Path.Combine(directory, "conan"),
                CompilerVersion = "15"
            };

            await RunCommand(ResourceUtils.ConanShim, project);

            _dialogService.Verify(x => x.ShowPluginError(It.IsAny<string>()), Times.Never);
        }
    }
}
