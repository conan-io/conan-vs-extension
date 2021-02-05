using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Core.VCInterfaces;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Services
{
    internal class VcProjectService : IVcProjectService
    {
        public IVCProject GetActiveProject()
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

        private static IVCProject CreateVCProjectWrapper(Project project)
        {
            int version = VisualStudioVersion;
            string wrapperDLL;

            if (version == 14)
                wrapperDLL = "Conan.VisualStudio.VCProjectWrapper14.dll";
            else if (version >= 15)
                wrapperDLL = "Conan.VisualStudio.VCProjectWrapper15.dll";
            else
                throw new NotSupportedException($"unsupported Visual Studio version: {version}");

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, wrapperDLL);

            var instance = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(path,
                "Conan.VisualStudio.VCProjectWrapper.VCProjectWrapper",
                false, BindingFlags.Default, null, new[] { project }, null, null);

            return instance as IVCProject;
        }

        private static IVCProject AsVCProjectImpl(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return CreateVCProjectWrapper(project);
        }

        public IVCProject AsVCProject(Project project)
        {
            return AsVCProjectImpl(project);
        }

        private static IVCProject GetActiveProject(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projects = (object[])dte.ActiveSolutionProjects;
            return AsVCProjectImpl(projects.Cast<Project>().Where(IsCppProject).FirstOrDefault());
        }

        public ConanProject ExtractConanProject(IVCProject vcProject, ISettingsService settingsService)
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
                foreach (IVCConfiguration configuration in vcProject.Configurations)
                {
                    project.Configurations.Add(ExtractConanConfiguration(settingsService, configuration));
                }
            }
            return project;
        }

        public async Task<ConanProject> ExtractConanProjectAsync(IVCProject vcProject, ISettingsService settingsService) => await Task.Run(() =>
        {
            return ExtractConanProject(vcProject, settingsService);
        });

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

        private static string GetInstallationDirectoryImpl(ISettingsService settingsService, IVCConfiguration configuration)
        {
            string installPath = ".conan";
            if (settingsService != null)
            {
                installPath = configuration.Evaluate(settingsService.GetConanInstallationPath());
                if (!Path.IsPathRooted(installPath))
                    installPath = Path.Combine(configuration.ProjectDirectory, installPath);
                return installPath;
            }
            return Path.Combine(configuration.ProjectDirectory, installPath);
        }

        public string GetInstallationDirectory(ISettingsService settingsService, IVCConfiguration configuration)
        {
            return GetInstallationDirectoryImpl(settingsService, configuration);
        }

        private static int VisualStudioVersion
        {
            get
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");
                if (File.Exists(path))
                {
                    System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                    return fvi.ProductMajorPart;
                }
                throw new NotSupportedException($"Cannot detect Visual Studio version, file {path} missing");
            }
        }

        private static ConanConfiguration ExtractConanConfiguration(ISettingsService settingsService, IVCConfiguration configuration)
        {
            string installPath = GetInstallationDirectoryImpl(settingsService, configuration);

            return new ConanConfiguration
            {
                VSName = configuration.Name,
                Architecture = GetArchitecture(configuration.PlatformName),
                BuildType = GetBuildType(configuration.ConfigurationName),
                CompilerToolset = configuration.Toolset,
                CompilerVersion = VisualStudioVersion.ToString(),
                InstallPath = installPath,
                RuntimeLibrary = configuration.RuntimeLibrary
            };
        }
    }
}
