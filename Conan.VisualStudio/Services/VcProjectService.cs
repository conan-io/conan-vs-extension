using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Conan.VisualStudio.Core;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Services
{
    internal class VcProjectService : IVcProjectService
    {
        public VCProject GetActiveProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)Package.GetGlobalService(typeof(SDTE));
            return GetActiveProject(dte);
        }

        private static bool IsCppProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return project != null && project.CodeModel != null
                && (project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageMC
                    || project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageVC);
        }

        public bool IsConanProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return IsCppProject(project) && null != ConanPathHelper.GetNearestConanfilePath(AsVCProject(project).ProjectDirectory);
        }

        public VCProject AsVCProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return project.Object as VCProject;
        }

        private static VCProject GetActiveProject(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = (object[])dte.ActiveSolutionProjects;
            return projects.Cast<Project>().Where(IsCppProject).Select(p => { Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread(); return p.Object; }).OfType<VCProject>().FirstOrDefault();
        }

        public ConanProject ExtractConanProject(VCProject vcProject, ISettingsService settingsService)
        {
            var projectPath = ConanPathHelper.GetNearestConanfilePath(vcProject.ProjectDirectory); // TODO: Instead of nearest, use the one added to the project (be explicit)
            if (projectPath == null)
            {
                return null;
            }

            if (vcProject.ActiveConfiguration == null) // project unloaded
            {
                return null;
            }

            string projectConanConfig = ConanPathHelper.GetNearestConanConfig(vcProject.ProjectDirectory);
            var project = new ConanProject
            {
                Path = projectPath,
                ConfigFile = projectConanConfig
            };

            if (settingsService != null && settingsService.GetConanInstallOnlyActiveConfiguration())
            {
                project.Configurations.Add(ExtractConanConfiguration(settingsService, vcProject.ActiveConfiguration));
            }
            else
            {
                foreach (VCConfiguration configuration in vcProject.Configurations)
                {
                    project.Configurations.Add(ExtractConanConfiguration(settingsService, configuration));
                }
            }
            return project;
        }

        public async Task<ConanProject> ExtractConanProjectAsync(VCProject vcProject, ISettingsService settingsService) => await Task.Run(() =>
        {
            return ExtractConanProject(vcProject, settingsService);
        }).ConfigureAwait(true);

        internal static string GetArchitecture(string platformName)
        {
            switch (platformName)
            {
                case "Win32": return "x86";
                case "x64": return "x86_64";
                case "ARM": return "armv7";
                case "ARM64": return "armv8";
                default: throw new NotSupportedException($"Platform {platformName} is not supported by the Conan plugin");
            }
        }

        internal static string GetBuildType(string configurationName) => configurationName;

        private static string GetInstallationDirectoryImpl(ISettingsService settingsService, VCConfiguration configuration)
        {
            string installPath = ".conan";
            if (settingsService != null)
            {
                IVCRulePropertyStorage generalSettings = configuration.Rules.Item("ConfigurationGeneral");
                installPath = configuration.Evaluate(settingsService.GetConanInstallationPath());
                if (!Path.IsPathRooted(installPath))
                    installPath = Path.Combine(configuration.project.ProjectDirectory, installPath);
                return installPath;
            }
            return Path.Combine(configuration.project.ProjectDirectory, installPath);
        }

        public string GetInstallationDirectory(ISettingsService settingsService, VCConfiguration configuration)
        {
            return GetInstallationDirectoryImpl(settingsService, configuration);
        }

        private static string RuntimeLibraryToString(runtimeLibraryOption RuntimeLibrary)
        {
            switch (RuntimeLibrary)
            {
                case runtimeLibraryOption.rtMultiThreaded:
                    return "MT";
                case runtimeLibraryOption.rtMultiThreadedDebug:
                    return "MTd";
                case runtimeLibraryOption.rtMultiThreadedDLL:
                    return "MD";
                case runtimeLibraryOption.rtMultiThreadedDebugDLL:
                    return "MDd";
                default:
                    throw new NotSupportedException($"Runtime Library {RuntimeLibrary} is not supported by the Conan plugin");
            }
        }

        private static string ConanCompilerVersion()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");
            if (File.Exists(path))
            {
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                int verName = fvi.ProductMajorPart;
                return verName.ToString(CultureInfo.CurrentCulture);
            }
            throw new NotSupportedException($"Cannot detect compiler version, file {path} missing");
        }

        private static ConanConfiguration ExtractConanConfiguration(ISettingsService settingsService, VCConfiguration configuration)
        {
            IVCRulePropertyStorage generalSettings = configuration.Rules.Item("ConfigurationGeneral");
            var toolset = generalSettings.GetEvaluatedPropertyValue("PlatformToolset");
            string installPath = GetInstallationDirectoryImpl(settingsService, configuration);
            var VCCLCompilerTool = configuration.Tools.Item("VCCLCompilerTool");

            return new ConanConfiguration
            {
                VSName = configuration.Name,
                Architecture = GetArchitecture(configuration.Platform.Name),
                BuildType = GetBuildType(configuration.ConfigurationName),
                CompilerToolset = toolset,
                CompilerVersion = ConanCompilerVersion(),
                InstallPath = installPath,
                RuntimeLibrary = VCCLCompilerTool != null ? RuntimeLibraryToString(VCCLCompilerTool.RuntimeLibrary) : null
            };
        }
    }
}
