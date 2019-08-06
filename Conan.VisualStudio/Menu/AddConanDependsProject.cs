using System.ComponentModel.Design;
using Conan.VisualStudio.Services;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDependsProject : MenuCommandBase
    {
        protected override int CommandId => PackageIds.AddConanDependsProjectId;

        private readonly Core.IErrorListService _errorListService;
        private readonly IVcProjectService _vcProjectService;
        private readonly IConanService _conanService;

        public AddConanDependsProject(
            IMenuCommandService commandService,
            Core.IErrorListService errorListService,
            IVcProjectService vcProjectService,
            IConanService conanService) : base(commandService, errorListService)
        {
            _errorListService = errorListService;
            _vcProjectService = vcProjectService;
            _conanService = conanService;
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            _errorListService.Clear();
            var vcProject = _vcProjectService.GetActiveProject();
            if (vcProject == null)
            {
                _errorListService.WriteError("A C++ project with a conan file must be selected.");
                return;
            }

            bool success = await _conanService.InstallAsync(vcProject, null);
            if (success)
            {
                await _conanService.IntegrateAsync(vcProject);
            }
        }
    }
}
