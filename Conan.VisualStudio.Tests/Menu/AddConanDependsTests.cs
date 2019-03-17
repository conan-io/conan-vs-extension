using System;
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
        private readonly Mock<IErrorListService> _errorListService = new Mock<IErrorListService>();

        private static ConanProject NewTestProject(string path) => new ConanProject
        {
            Path = path,
            Configurations =
            {
                new ConanConfiguration
                {
                    CompilerVersion = "15",
                    InstallPath = Path.Combine(path, "conan"),
                }
            }
        };

        private Task RunCommandAsync(string conanPath, ConanProject project)
        {
            var vcProject = Mock.Of<VCProject>();
            Mock.Get(vcProject).Setup(x => x.Name).Returns("TestProject");

            var commandService = Mock.Of<IMenuCommandService>();

            var projectService = new Mock<IVcProjectService>();
            projectService.Setup(x => x.GetActiveProject()).Returns(vcProject);
            projectService.Setup(x => x.ExtractConanProjectAsync(It.IsAny<VCProject>(), null)).ReturnsAsync(project);

            var settingsService = new Mock<ISettingsService>();
            settingsService.Setup(x => x.GetConanExecutablePath()).Returns(conanPath);

            var serviceProvider = new Mock<IServiceProvider>();

            var command = new AddConanDependsProject(
                commandService,
                _errorListService.Object,
                projectService.Object,
                Mock.Of<IConanService>());
            return command.MenuItemCallbackAsync();
        }

        [Fact(Skip = "failing because of ThreadHelper, we're not going to test GUI")]
        public async Task AddConanDependsShowsAnErrorWindowIfConanReturnsExitCodeAsync()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            var project = NewTestProject(directory);

            await RunCommandAsync(ResourceUtils.ConanShimError, project);

            const int exitCode = 10;
            var logFilePath = Path.Combine(directory, "conan", "conan.log");

            _errorListService.Verify(
                x => x.WriteError(
                    $"Conan has returned exit code '{exitCode}' " +
                    $"while processing configuration '{project.Configurations.Single()}'. " +
                    $"Please check file '{logFilePath}' for details.", logFilePath));
            var logContent = File.ReadAllText(logFilePath);
            Assert.NotEmpty(logContent);
        }

        [Fact(Skip = "failing because of ThreadHelper, we're not going to test GUI")]
        public async Task AddConanDependsSuccedsIfLogDirectoryDoesNotExistsAsync()
        {
            var directory = FileSystemUtils.CreateTempDirectory();
            var project = NewTestProject(directory);

            await RunCommandAsync(ResourceUtils.ConanShim, project);

            _errorListService.Verify(x => x.WriteError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
