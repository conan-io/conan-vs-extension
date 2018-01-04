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

        public IntegrateIntoProjectCommand(IMenuCommandService commandService, IDialogService dialogService)
            : base(commandService, dialogService)
        {
        }

        protected override Task MenuItemCallback()
        {
            var project = VcProjectService.GetActiveProject();
            return VcProjectService.AddPropsImport(project.ProjectFile, @"conan\conanbuildinfo_multi.props");
        }
    }
}
