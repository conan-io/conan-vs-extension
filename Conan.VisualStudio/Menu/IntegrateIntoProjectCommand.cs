using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
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
        private readonly IDialogService _dialogService;

        public IntegrateIntoProjectCommand(
            IMenuCommandService commandService,
            IDialogService dialogService,
            IVcProjectService vcProjectService) : base(commandService, dialogService)
        {
            _vcProjectService = vcProjectService;
            _dialogService = dialogService;
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            var project = _vcProjectService.GetActiveProject();
            var projectDirectory = project.ProjectDirectory;
            var conanfileDirectory = await ConanPathHelper.GetNearestConanfilePath(projectDirectory);
            if (conanfileDirectory == null)
            {
                _dialogService.ShowPluginError("unable to locate conanfile directory!");
                return;
            }
            var propFilePath = Path.Combine(conanfileDirectory, @".conan\conanbuildinfo_multi.props");
            var relativePropFilePath = ConanPathHelper.GetRelativePath(projectDirectory, propFilePath);
            await _vcProjectService.AddPropsImportAsync(project.ProjectFile, relativePropFilePath);
        }
    }
}
