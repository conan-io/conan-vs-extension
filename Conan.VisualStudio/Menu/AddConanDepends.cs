using System;
using System.ComponentModel.Design;
using System.IO;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Extensions;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDepends : MenuCommandBase
    {
        protected override int CommandId => 0x0100;

        private readonly Package _package;
        private readonly IDialogService _dialogService;

        public AddConanDepends(Package package, IMenuCommandService commandService, IDialogService dialogService)
            : base(commandService, dialogService)
        {
            _package = package;
            _dialogService = dialogService;
        }

        protected override async Task MenuItemCallback()
        {
            var vcProject = VcProjectService.GetActiveProject();
            if (vcProject == null)
            {
                _dialogService.ShowPluginError("A C++ project with a conan file must be selected.");
                return;
            }

            if (!_dialogService.ShowOkCancel($"Process conanbuild.txt for '{vcProject.Name}'?\n"))
                return;

            var conanPath = _package.GetConanExecutablePath();
            if (conanPath == null)
            {
                _dialogService.ShowPluginError(
                    "Conan executable path is not set and Conan executable wasn't found automatically. " +
                    "Please set it up in the Tools → Settings → Conan menu.");
                return;
            }

            var conan = new ConanRunner(conanPath);
            var project = await VcProjectService.ExtractConanConfiguration(vcProject);
            await InstallDependencies(conan, project);
        }

        private async Task InstallDependencies(ConanRunner conan, ConanProject project)
        {
            try
            {
                var process = await conan.Install(project);
                using (var reader = process.StandardOutput)
                {
                    var result = reader.ReadToEnd();
                    Console.Write(result);
                }

                process.WaitForExit();
            }
            catch (FileNotFoundException)
            {
                _dialogService.ShowPluginError("Could not locate conan on execution path.");
            }
        }
    }
}
