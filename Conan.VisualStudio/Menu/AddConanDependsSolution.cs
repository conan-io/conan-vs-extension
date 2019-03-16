using System.ComponentModel.Design;
using System.Threading.Tasks;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using System;
using EnvDTE;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDependsSolution : MenuCommandBase
    {
        protected override int CommandId => 0x0101;

        private readonly IDialogService _dialogService;
        private readonly IVcProjectService _vcProjectService;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConanService _conanService;

        public AddConanDependsSolution(
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
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
            foreach (Project project in dte.Solution.Projects)
            {
                if (_vcProjectService.IsConanProject(project))
                {
                    await _conanService.InstallAsync(_vcProjectService.AsVCProject(project));
                    await _conanService.IntegrateAsync(_vcProjectService.AsVCProject(project));
                }
            }

            await TaskScheduler.Default;
        }
    }
}
