using System.ComponentModel.Design;
using System.Threading.Tasks;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE80;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using System;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class AddConanDependsConanfile : MenuCommandBase
    {
        protected override int CommandId => 0x0102;

        private readonly IErrorListService _errorListService;
        private readonly IVcProjectService _vcProjectService;
        private readonly IConanService _conanService;
        private readonly DTE2 _dte2;

        public AddConanDependsConanfile(
            IMenuCommandService commandService,
            IErrorListService errorListService,
            IVcProjectService vcProjectService,
            IConanService conanService) : base(commandService, errorListService)
        {
            _errorListService = errorListService;
            _vcProjectService = vcProjectService;
            _conanService = conanService;
            _dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
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

            await _conanService.InstallAsync(vcProject);
            await _conanService.IntegrateAsync(vcProject);
        }

        protected override void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OleMenuCommand command = sender as OleMenuCommand;
            command.Visible = false;
            if (null != _dte2)
            {
                object[] selectedItems = (object[])_dte2.ToolWindows.SolutionExplorer.SelectedItems;

                if (selectedItems.Length == 1)
                {
                    EnvDTE.UIHierarchyItem uIHierarchyItem = selectedItems[0] as EnvDTE.UIHierarchyItem;
                    if (uIHierarchyItem.Object is EnvDTE.ProjectItem projectItem && VSConanPackage.IsConanfile(projectItem.Name))
                        command.Visible = true;
                }
            }
        }
    }
}
