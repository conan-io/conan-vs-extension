using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio
{
    class CLISwitchRunInstall : IVsUpdateSolutionEvents3
    {
        public const string Name = "ConanRunInstall";
        private readonly BuildEvents _buildEvents;
        private readonly DTE _dte;
        private readonly IVcProjectService _vcProjectService;
        private IConanService _conanService;
        private string _conanPath;

        public CLISwitchRunInstall(
            DTE dte,
            IVsSolutionBuildManager3 service,
            IVcProjectService vcProjectService,
            IConanService conanService,
            string conanPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dte = dte;
            _vcProjectService = vcProjectService;
            _conanService = conanService;
            _conanPath = conanPath;

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildProjConfigBegin += OnBuildProjConfigBegin;

            service.AdviseUpdateSolutionEvents3(this, out uint cookie);
        }

        private void OnBuildProjConfigBegin(string Project, string ProjectConfig, string Platform, string SolutionConfig)
        {
            System.Console.WriteLine($"Running Conan install for '{Project}' as requested from command line.");
            foreach (Project project in _dte.Solution.Projects)
            {
                if (project.UniqueName == Project)
                {
                    VCProject vcProject = _vcProjectService.AsVCProject(project);
                    ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        bool success = await _conanService.InstallAsync(vcProject, _conanPath);
                        if (success)
                        {
                            await _conanService.IntegrateAsync(vcProject);
                        }
                    });
                }
            }
        }

        public int OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return VSConstants.S_OK;
        }
    }
}
