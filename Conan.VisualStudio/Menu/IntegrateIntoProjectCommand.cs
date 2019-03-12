using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.Menu
{
    /// <summary>
    /// Command to install the Conan prop files into the project file.
    /// </summary>
    internal sealed class IntegrateIntoProjectCommand : MenuCommandBase
    {
        protected override int CommandId => 4130;

        private readonly IVcProjectService _vcProjectService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private readonly IConanService _conanService;

        public IntegrateIntoProjectCommand(
            IMenuCommandService commandService,
            IDialogService dialogService,
            IVcProjectService vcProjectService,
            ISettingsService settingsService,
            IConanService conanService) : base(commandService, dialogService)
        {
            _vcProjectService = vcProjectService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            _conanService = conanService;
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            var project = _vcProjectService.GetActiveProject();
            if (project == null)
            {
                _dialogService.ShowPluginError("A C++ project with a conan file must be selected.");
                return;
            }
            await _conanService.IntegrateAsync(project);
        }
    }
}
