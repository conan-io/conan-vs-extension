using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Conan.VisualStudio.Services
{
    public class SolutionEventsHandler : IVsSolutionEvents
    {
        private readonly VisualStudioSettingsService _settingsService;
        private readonly string _conanPath;

        public SolutionEventsHandler(VSConanPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _settingsService = new VisualStudioSettingsService(package);
            _conanPath = _settingsService.GetConanExecutablePath();  
        }

        /// <summary>
        /// Convert VsHierarchy into a EnvDTE Project
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <returns></returns>
        private EnvDTE.Project GetProject(IVsHierarchy pHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            return objProj as EnvDTE.Project;
        }

        /// <summary>
        /// Get the active configuration for a project and log Name and PlatformName to a Output Window
        /// </summary>
        /// <param name="project">EnvDTE Project</param>
        private void OutputActiveConfiguration(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var config = project.ConfigurationManager.ActiveConfiguration;
            var output = "Loaded project '" + project.Name + "': '" + config.PlatformName + "' - '" + config.ConfigurationName + "'";
            Logger.Log(output);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}
