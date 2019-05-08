using Conan.VisualStudio.Core;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Diagnostics;
using System.IO;

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

        private async System.Threading.Tasks.Task InspectAsync(EnvDTE.Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var conanProject = new ConanProject
            {
                Path = project.FileName
            };

            await TaskScheduler.Default;

            var conanRunner = new ConanRunner(_settingsService.LoadSettingFile(conanProject), _conanPath);

            var process = await conanRunner.Inspect(conanProject);

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
            ThreadHelper.ThrowIfNotOnUIThread();

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

            ThreadHelper.ThrowIfNotOnUIThread();

            var vcProject = project.Object as VCProject;

            ThreadHelper.JoinableTaskFactory.RunAsync(
                async delegate
                {
                    await InspectAsync(project);
                }
            );

            foreach (Property property in project.Properties)
            {
                try
                {
                    Logger.Log(property.Name + " = " + property.Value);
                }
                catch (Exception)
                {
                    // Let it go, Let it go
                }               
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
