using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using System;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDepends : MenuCommandBase
    {
        protected override int CommandId => 0x0100;

        private readonly IDialogService _dialogService;
        private readonly IVcProjectService _vcProjectService;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;

        public AddConanDepends(
            IMenuCommandService commandService,
            IDialogService dialogService,
            IVcProjectService vcProjectService,
            ISettingsService settingsService,
            IServiceProvider serviceProvider) : base(commandService, dialogService)
        {
            _dialogService = dialogService;
            _vcProjectService = vcProjectService;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;
        }

        protected internal override async Task MenuItemCallbackAsync()
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

            var project = await _vcProjectService.ExtractConanProjectAsync(vcProject, _settingsService);
            if (project == null)
            {
                _dialogService.ShowPluginError("Unable to extract conan project!");
                return;
            }
            var conan = new ConanRunner(_settingsService.LoadSettingFile(project), conanPath);           

            await InstallDependenciesAsync(conan, project);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var infoBarService = new InfobarService(_serviceProvider);
            infoBarService.ShowInfoBar(vcProject);

            await TaskScheduler.Default;
        }

        private async Task InstallDependenciesAsync(ConanRunner conan, ConanProject project)
        {
            var installPath = project.InstallPath;
            await Task.Run(() => Directory.CreateDirectory(installPath));
            var logFilePath = Path.Combine(installPath, $"conan_{Guid.NewGuid().ToString()}.log");

            using (var logFile = File.Open(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var logStream = new StreamWriter(logFile))
            {
                foreach (var configuration in project.Configurations)
                {
                    ConanGeneratorType generator = _settingsService.GetConanGenerator();
                    var process = await conan.Install(project, configuration, generator);

                    Logger.Log(
                        $"[Conan.VisualStudio] Calling process '{process.StartInfo.FileName}' " +
                        $"with arguments '{process.StartInfo.Arguments}'");

                    await logStream.WriteLineAsync(
                        $"[Conan.VisualStudio] Calling process '{process.StartInfo.FileName}' " +
                        $"with arguments '{process.StartInfo.Arguments}'");
                    using (var reader = process.StandardOutput)
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            await logStream.WriteLineAsync(line);

                            Logger.Log(line);
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
