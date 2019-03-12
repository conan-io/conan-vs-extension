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
        private readonly IConanService _conanService;

        public AddConanDepends(
            IMenuCommandService commandService,
            IDialogService dialogService,
            IVcProjectService vcProjectService,
            ISettingsService settingsService,
            IServiceProvider serviceProvider,
            IConanService conanService) : base(commandService, dialogService)
        {
            _dialogService = dialogService;
            _vcProjectService = vcProjectService;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;
            _conanService = conanService;
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

            await _conanService.InstallAsync(vcProject);
            await _conanService.IntegrateAsync(vcProject);

            _dialogService.ShowInfo("Conan dependencies have been installed successfully.");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var infoBarService = new InfobarService(_serviceProvider);
            infoBarService.ShowInfoBar(vcProject);

            await TaskScheduler.Default;
        }
    }
}
