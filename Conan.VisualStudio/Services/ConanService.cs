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

namespace Conan.VisualStudio.Services
{
    internal class ConanService : IConanService
    {
        private readonly ISettingsService _settingsService;
        private readonly IErrorListService _errorListService;
        private readonly IVcProjectService _vcProjectService;
        private readonly IVsSolution4 _solution;
        private List<string> _refreshingProjects;

        List<string> IConanService.RefreshingProjects => _refreshingProjects;

        public ConanService(ISettingsService settingsService, Core.IErrorListService errorListService, IVcProjectService vcProjectService, IVsSolution4 solution)
        {
            _settingsService = settingsService;
            _errorListService = errorListService;
            _vcProjectService = vcProjectService;
            _solution = solution;
            _refreshingProjects = new List<string>();
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


        private bool IntegrateIntoConfiguration(IVCConfiguration configuration)
        {
            string absPropFilePath = GetPropsFilePath(configuration);
            string relativePropFilePath = ConanPathHelper.GetRelativePath(configuration.ProjectDirectory, absPropFilePath);

            configuration.AdditionalDependencies = configuration.AdditionalDependencies.Replace("$(NOINHERIT)", "");

            bool bProjectMustBeRefreshed = configuration.AddPropertySheet(relativePropFilePath);
            if(bProjectMustBeRefreshed)
                Logger.Log($"[Conan.VisualStudio] Property sheet '{absPropFilePath}' added (or updated) to project {configuration.ProjectName}");
            else
                Logger.Log($"[Conan.VisualStudio] Property sheet '{absPropFilePath}' already added to project {configuration.ProjectName}");
            configuration.CollectIntelliSenseInfo();
            return bProjectMustBeRefreshed;
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

            bool bMustBeRefreshed = false;
            if (_settingsService.GetConanInstallOnlyActiveConfiguration())
            {
                bMustBeRefreshed = IntegrateIntoConfiguration(vcProject.ActiveConfiguration);
            }
            else
            {
                foreach (IVCConfiguration configuration in vcProject.Configurations)
                {
                    bMustBeRefreshed |= IntegrateIntoConfiguration(configuration);
                }
            }
            Guid guid = new Guid(vcProject.Guid);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (bMustBeRefreshed)
            {
                _refreshingProjects.Add(vcProject.FullPath);
                _solution.UnloadProject(guid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
                _solution.ReloadProject(guid);
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
