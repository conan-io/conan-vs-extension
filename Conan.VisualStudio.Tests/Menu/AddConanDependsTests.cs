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
            var vcProject = Mock.Of<VCProject>();
            Mock.Get(vcProject).Setup(x => x.Name).Returns("TestProject");

            var commandService = Mock.Of<IMenuCommandService>();
            var dialogService = new Mock<IDialogService>();
            dialogService.Setup(x => x.ShowOkCancel(It.IsAny<string>())).Returns(true);

            var projectService = new Mock<IVcProjectService>();
            projectService.Setup(x => x.GetActiveProject()).Returns(vcProject);
            projectService.Setup(x => x.ExtractConanConfiguration(It.IsAny<VCProject>())).ReturnsAsync(project);

            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(x => x.GetConanExecutablePath()).Returns(ResourceUtils.ConanShimError);

            var command = new AddConanDepends(
                commandService,
                dialogService.Object,
                projectService.Object,
                settingsService.Object);
            await command.MenuItemCallback();

            const int exitCode = 10;
            var logFilePath = Path.Combine(directory, "conan.log");
            dialogService.Verify(
                x => x.ShowPluginError(
                    $"Conan has returned exit code {exitCode}. Please check file '{logFilePath}' for details."));
            var logContent = File.ReadAllText(Path.Combine(directory, "conan.log"));
            Assert.Equal($"conan-shim-error{Environment.NewLine}", logContent);
        }
    }
}
