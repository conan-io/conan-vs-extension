using System.ComponentModel.Design;
using System.IO;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDepends : MenuCommandBase
    {
        protected override int CommandId => 0x0100;

        private readonly IDialogService _dialogService;
        private readonly IVcProjectService _vcProjectService;
        private readonly ISettingsService _settingsService;

        public AddConanDepends(
            IMenuCommandService commandService,
            IDialogService dialogService,
            IVcProjectService vcProjectService,
            ISettingsService settingsService) : base(commandService, dialogService)
        {
            _dialogService = dialogService;
            _vcProjectService = vcProjectService;
            _settingsService = settingsService;
        }

        protected internal override async Task MenuItemCallback()
        {
            var vcProject = _vcProjectService.GetActiveProject();
            if (vcProject == null)
            {
                _dialogService.ShowPluginError("A C++ project with a conan file must be selected.");
                return;
            }

            if (!_dialogService.ShowOkCancel($"Process conanbuild.txt for '{vcProject.Name}'?\n"))
                return;

            var conanPath = _settingsService.GetConanExecutablePath();
            if (conanPath == null)
            {
                _dialogService.ShowPluginError(
                    "Conan executable path is not set and Conan executable wasn't found automatically. " +
                    "Please set it up in the Tools → Settings → Conan menu.");
                return;
            }

            var conan = new ConanRunner(conanPath);
            var project = await _vcProjectService.ExtractConanProject(vcProject);
            await InstallDependencies(conan, project);
        }

        private async Task InstallDependencies(ConanRunner conan, ConanProject project)
        {
            var installPath = project.InstallPath;
            await Task.Run(() => Directory.CreateDirectory(installPath));
            var logFilePath = Path.Combine(installPath, "conan.log");

            using (var logFile = File.Open(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var logStream = new StreamWriter(logFile))
            {
                foreach (var configuration in project.Configurations)
                {
                    var process = await conan.Install(project, configuration);

                    await logStream.WriteLineAsync(
                        $"[Conan.VisualStudio] Calling process '{process.StartInfo.FileName}' " +
                        $"with arguments '{process.StartInfo.Arguments}'");
                    using (var reader = process.StandardOutput)
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            await logStream.WriteLineAsync(line);
                        }
                    }

                    var exitCode = await process.WaitForExitAsync();
                    if (exitCode != 0)
                    {
                        _dialogService.ShowPluginError(
                            $"Conan has returned exit code '{exitCode}' " +
                            $"while processing configuration '{configuration}'. " +
                            $"Please check file '{logFilePath}' for details.");
                        return;
                    }
                }

                _dialogService.ShowInfo("Conan dependencies have been installed successfully.");
            }
        }
    }
}
