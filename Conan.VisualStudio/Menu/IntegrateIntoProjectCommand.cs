using System.ComponentModel.Design;
using System.Threading.Tasks;
using Conan.VisualStudio.Services;

namespace Conan.VisualStudio.Menu
{
    /// <summary>
    /// Command to install the Conan prop files into the project file.
    /// </summary>
    internal sealed class IntegrateIntoProjectCommand : MenuCommandBase
    {
        protected override int CommandId => 4130;

        private readonly IVcProjectService _vcProjectService;

        public IntegrateIntoProjectCommand(
            IMenuCommandService commandService,
            IDialogService dialogService,
            IVcProjectService vcProjectService) : base(commandService, dialogService)
        {
            _vcProjectService = vcProjectService;
        }

        protected internal override Task MenuItemCallback()
        {
            var project = _vcProjectService.GetActiveProject();
            return _vcProjectService.AddPropsImport(project.ProjectFile, @"conan\conanbuildinfo_multi.props");
        }
    }
}
