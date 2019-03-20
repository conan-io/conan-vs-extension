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
        protected override int CommandId => PackageIds.AddConanDependsSolutiontId;

        private readonly IVcProjectService _vcProjectService;
        private readonly IErrorListService _errorListService;
        private readonly IConanService _conanService;

        public AddConanDependsSolution(
            IMenuCommandService commandService,
            IErrorListService errorListService,
            IVcProjectService vcProjectService,
            IConanService conanService) : base(commandService, errorListService)
        {
            _vcProjectService = vcProjectService;
            _errorListService = errorListService;
            _conanService = conanService;
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _errorListService.Clear();

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
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
