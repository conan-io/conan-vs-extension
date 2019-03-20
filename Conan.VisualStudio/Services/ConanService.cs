using System;
using System.IO;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.Services
{
    internal class ConanService : IConanService
    {
        private readonly ISettingsService _settingsService;
        private readonly IErrorListService _errorListService;
        private readonly IVcProjectService _vcProjectService;

        public ConanService(ISettingsService settingsService, IErrorListService errorListService, IVcProjectService vcProjectService)
        {
            _settingsService = settingsService;
            _errorListService = errorListService;
            _vcProjectService = vcProjectService;
        }

        private string GetPropsFilePath(VCConfiguration configuration)
        {
            string installPath = _vcProjectService.GetInstallationDirectory(_settingsService, configuration);
            string propFileName;
            if (_settingsService.GetConanGenerator() == ConanGeneratorType.visual_studio)
                propFileName = @"conanbuildinfo.props";
            else
                propFileName = @"conanbuildinfo_multi.props";
            string propFilePath = Path.Combine(installPath, propFileName);
            return ConanPathHelper.GetRelativePath(configuration.project.ProjectDirectory, propFilePath);
        }

        public async Task IntegrateAsync(VCProject vcProject)
        {
            var projectDirectory = vcProject.ProjectDirectory;
            var conanfileDirectory = await ConanPathHelper.GetNearestConanfilePathAsync(projectDirectory);
            if (conanfileDirectory == null)
            {
                _errorListService.WriteError("unable to locate conanfile directory!");
                return;
            }

            if (_settingsService.GetConanInstallOnlyActiveConfiguration())
            {
                string relativePropFilePath = GetPropsFilePath(vcProject.ActiveConfiguration);
                vcProject.ActiveConfiguration.AddPropertySheet(relativePropFilePath);
                vcProject.ActiveConfiguration.CollectIntelliSenseInfo();
            }
            else
            {
                foreach (VCConfiguration configuration in vcProject.Configurations)
                {
                    string relativePropFilePath = GetPropsFilePath(configuration);
                    configuration.AddPropertySheet(relativePropFilePath);
                    configuration.CollectIntelliSenseInfo();
                }
            }
        }

        public async Task InstallAsync(VCProject vcProject)
        {
            var conanPath = _settingsService.GetConanExecutablePath();
            if (conanPath == null)
            {
                _errorListService.WriteError(
                    "Conan executable path is not set and Conan executable wasn't found automatically. " +
                    "Please set it up in the Tools → Settings → Conan menu.");
                return;
            }

            var project = await _vcProjectService.ExtractConanProjectAsync(vcProject, _settingsService);
            if (project == null)
            {
                _errorListService.WriteError("Unable to extract conan project!");
                return;
            }
            var conan = new ConanRunner(_settingsService.LoadSettingFile(project), conanPath);

            await InstallDependenciesAsync(conan, project);
        }

        private async Task InstallDependenciesAsync(ConanRunner conan, ConanProject project)
        {
            foreach (var configuration in project.Configurations)
            {
                var installPath = configuration.InstallPath;
                await Task.Run(() => Directory.CreateDirectory(installPath));
                var logFilePath = Path.Combine(installPath, $"conan_{Guid.NewGuid().ToString()}.log");

                using (var logFile = File.Open(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var logStream = new StreamWriter(logFile))
                {
                    ConanGeneratorType generator = _settingsService.GetConanGenerator();
                    ConanBuildType build = _settingsService.GetConanBuild();
                    bool update = _settingsService.GetConanUpdate();
                    var process = await conan.Install(project, configuration, generator, build, update);

                    string message = $"[Conan.VisualStudio] Calling process '{process.StartInfo.FileName}' " +
                        $"with arguments '{process.StartInfo.Arguments}'";

                    Logger.Log(message);
                    await logStream.WriteLineAsync(message);

                    using (var reader = process.StandardOutput)
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            await logStream.WriteLineAsync(line);

                            Logger.Log(line);
                        }
                    }

                    var exitCode = await process.WaitForExitAsync();
                    if (exitCode != 0)
                    {
                        message = $"Conan has returned exit code '{exitCode}' " +
                            $"while processing configuration '{configuration}'. " +
                            $"Please check file '{logFilePath}' for details.";

                        Logger.Log(message);
                        await logStream.WriteLineAsync(message);
                        _errorListService.WriteError(message, logFilePath);
                        return;
                    }
                    else
                    {
                        message = $"[Conan.VisualStudio] Conan has succsessfully installed configuration '{configuration}'";
                        Logger.Log(message);
                        await logStream.WriteLineAsync(message);
                        _errorListService.WriteMessage(message);
                    }
                }
            }
        }
    }
}
