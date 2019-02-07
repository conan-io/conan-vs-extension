using Conan.VisualStudio.Core;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;

namespace Conan.VisualStudio.Services
{
    public class SolutionEventsHandler : IVsSolutionEvents
    {
        private ConanRunner _conanRunner;

        public SolutionEventsHandler(VSConanPackage package)
        {
            var settingsService = new VisualStudioSettingsService(package);
            var conanPath = settingsService.GetConanExecutablePath();
            _conanRunner = new ConanRunner(conanPath);
        }

        /// <summary>
        /// Convert VsHierarchy into a EnvDTE Project
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <returns></returns>
        private EnvDTE.Project GetProject(IVsHierarchy pHierarchy)
        {
            pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            return objProj as EnvDTE.Project;
        }

        /// <summary>
        /// Get the active configuration for a project and log Name and PlatformName to a Output Window
        /// </summary>
        /// <param name="project">EnvDTE Project</param>
        private void OutputActiveConfiguration(EnvDTE.Project project)
        {
            var config = project.ConfigurationManager.ActiveConfiguration;
            var output = "Loaded project '" + project.Name + "': '" + config.PlatformName + "' - '" + config.ConfigurationName + "'";
            Logger.Log(output);
        }

        private async void Inspect(EnvDTE.Project project)
        {
            var conanProject = new ConanProject
            {
                Path = project.FileName
            };

            var process = await _conanRunner.Inspect(conanProject);

            Logger.Log(
                $"[Conan.VisualStudio] Calling process '{process.StartInfo.FileName}' " +
                $"with arguments '{process.StartInfo.Arguments}'");
            using (var reader = process.StandardOutput)
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Logger.Log(line);
                }
            }
        }

        private void RunCsi(Project project)
        {
            var vsPath = Path.GetDirectoryName(project.DTE.FullName);
            var projectDir = Path.GetDirectoryName(project.FileName);
            var csi = Path.Combine(vsPath, "..\\..\\MSBuild\\15.0\\Bin\\Roslyn\\csi.exe");
            var csiScript = Path.Combine(projectDir, "OnAfterLoadProject.csx");

            var startInfo = new ProcessStartInfo
            {
                FileName = csi,
                Arguments = csiScript,
                UseShellExecute = false,
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(startInfo);
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
            var project = GetProject(pRealHierarchy);
            OutputActiveConfiguration(project);

            Inspect(project);

            foreach (Property property in project.Properties)
            {
                Logger.Log(property.Name + " = " + property.Value);
            }

            RunCsi(project);

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
