using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Core.VCInterfaces;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Windows.Forms;
using EnvDTE;

namespace Conan.VisualStudio.Services
{
    internal class ConanService : IConanService
    {
        private readonly ISettingsService _settingsService;
        private readonly IErrorListService _errorListService;
        private readonly IVcProjectService _vcProjectService;
        private readonly IVsSolution4 _solution;
        private readonly DTE _dte;
        private List<string> _refreshingProjects;

        List<string> IConanService.RefreshingProjects => _refreshingProjects;

        public ConanService(ISettingsService settingsService, Core.IErrorListService errorListService, IVcProjectService vcProjectService, IVsSolution4 solution, DTE dte)
        {
            _settingsService = settingsService;
            _errorListService = errorListService;
            _vcProjectService = vcProjectService;
            _solution = solution;
            _refreshingProjects = new List<string>();
            _dte = dte;
        }

        private string GetPropsFilePath(IVCConfiguration configuration)
        {
            string installPath = _vcProjectService.GetInstallationDirectory(_settingsService, configuration);
            string propFileName;
            if (_settingsService.GetConanGenerator() == ConanGeneratorType.visual_studio)
                propFileName = @"conanbuildinfo.props";
            else
                propFileName = @"conanbuildinfo_multi.props";
            return Path.Combine(installPath, propFileName);
        }


        private void IntegrateIntoConfiguration(IVCConfiguration configuration, IVsSolution4 solution, ref IVCProject vcProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string projectName = configuration.ProjectName;
            string projectFullName = Path.Combine(configuration.ProjectDirectory, configuration.ProjectFileName);
            string absPropFilePath = GetPropsFilePath(configuration);
            string relativePropFilePath = ConanPathHelper.GetRelativePath(configuration.ProjectDirectory, absPropFilePath);

            configuration.AdditionalDependencies = configuration.AdditionalDependencies.Replace("$(NOINHERIT)", "");

            bool bPropPresent = configuration.IsPropertySheetPresent(relativePropFilePath);
            if (!bPropPresent)
            {
                Guid guid = new Guid(vcProject.Guid);
                string vcProjectFullPath = vcProject.FullPath;
                _refreshingProjects.Add(vcProjectFullPath);
                solution.UnloadProject(guid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
                //From this point, vcProject and configuration properties that use it are no more valid
                configuration.AddPropertySheet(relativePropFilePath, vcProjectFullPath);
                solution.ReloadProject(guid);
                //Recreate vcProject
                foreach (Project project in _dte.Solution.Projects)
                {
                    if (project.FullName == projectFullName)
                    {
                        vcProject = _vcProjectService.AsVCProject(project);
                    }
                }

                Logger.Log($"[Conan.VisualStudio] Property sheet '{absPropFilePath}' added (or updated) to project {projectName}");
            }
            else
            {
                Logger.Log($"[Conan.VisualStudio] Property sheet '{absPropFilePath}' already added to project {projectName}");
                configuration.CollectIntelliSenseInfo();
            }
        }

        public async System.Threading.Tasks.Task IntegrateAsync(IVCProject vcProject)
        {
            if(!vcProject.Saved)
            {
                DialogResult res = MessageBox.Show(
                    "Project must be saved to finish conan install. Do you want to save it? If you choose 'No' it will not be saved and conan install could be unsuccessful for this session.",
                    "Save project?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (res == DialogResult.No)
                    return;

                vcProject.Save();
            }
            var projectDirectory = vcProject.ProjectDirectory;
            var conanfileDirectory = await ConanPathHelper.GetNearestConanfilePathAsync(projectDirectory);
            if (conanfileDirectory == null)
            {
                _errorListService.WriteError("unable to locate conanfile directory!");
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_settingsService.GetConanInstallOnlyActiveConfiguration())
            {
                IntegrateIntoConfiguration(vcProject.ActiveConfiguration, _solution, ref vcProject);
            }
            else
            {
                //foreach is not possible here since vcProject could be reinitialized during IntegrateIntoConfiguration
                int nNbConfigurations = vcProject.Configurations.Count;
                for (int i = 0; i < nNbConfigurations; i++)
                {
                    IVCConfiguration configuration = vcProject.Configurations[i];
                    IntegrateIntoConfiguration(configuration, _solution, ref vcProject);
                }
            }
        }

        public async Task<bool> InstallAsync(IVCProject vcProject)
        {
            var conanPath = _settingsService.GetConanExecutablePath();
            if (conanPath == null || conanPath == "")
            {
                _errorListService.WriteError(
                    "Conan executable path is not set and Conan executable wasn't found automatically. " +
                    "Please set it up in the Tools → Settings → Conan menu.");
                return false;
            }

            var project = await _vcProjectService.ExtractConanProjectAsync(vcProject, _settingsService);
            if (project == null)
            {
                _errorListService.WriteError("Unable to extract conan project!");
                return false;
            }
            var conan = new ConanRunner(conanPath);

            return await InstallDependenciesAsync(conan, project);
        }

        private async Task<bool> InstallDependenciesAsync(ConanRunner conan, ConanProject project)
        {
            foreach (var configuration in project.Configurations)
            {
                var installPath = configuration.InstallPath;
                await System.Threading.Tasks.Task.Run(() => Directory.CreateDirectory(installPath));
                var logFilePath = Path.Combine(installPath, $"conan_{Guid.NewGuid().ToString()}.log");

                using (var logFile = File.Open(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var logStream = new StreamWriter(logFile))
                {
                    ConanGeneratorType generator = _settingsService.GetConanGenerator();
                    ConanBuildType build = _settingsService.GetConanBuild();
                    bool update = _settingsService.GetConanUpdate();

                    ProcessStartInfo process = null;
                    try
                    {
                        // Run 'conan --version' for log purposes 
                        process = conan.Version();
                        int exitCode = await Utils.RunProcessAsync(process, logStream);
                        if (exitCode != 0)
                        {
                            string message = "Cannot get Conan version, check that the " +
                                "executable is pointing to a valid one";
                            Logger.Log(message);
                            await logStream.WriteLineAsync(message);
                            _errorListService.WriteError(message, logFilePath);
                        }

                        // Run the install
                        process = conan.Install(project, configuration, generator, build, update, _errorListService);
                        exitCode = await Utils.RunProcessAsync(process, logStream);
                        if (exitCode != 0)
                        {
                            string message = $"Conan has returned exit code '{exitCode}' " +
                                      $"while processing configuration '{configuration}'. " +
                                      $"Please check file '{logFilePath}' for details.";

                            Logger.Log(message);
                            await logStream.WriteLineAsync(message);
                            _errorListService.WriteError(message, logFilePath);
                            return false;
                        }
                        else
                        {
                            string message = $"[Conan.VisualStudio] Conan has succsessfully " +
                                      $"installed configuration '{configuration}'";
                            Logger.Log(message);
                            await logStream.WriteLineAsync(message);
                            _errorListService.WriteMessage(message);
                        }
                    }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        string message = $"[Conan.VisualStudio] Unhandled error running '{process.FileName}'" +
                                  $": {e.Message}. Check log file '{logFilePath}' for details";
                        Logger.Log(message);
                        await logStream.WriteLineAsync(message);
                        _errorListService.WriteError(message);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
