using System;
using System.Collections.Generic;
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

        private static VCProject GetActiveProject(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            bool IsCppProject(Project project) =>
                project != null
                && (project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageMC
                    || project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageVC);

            var projects = (object[])dte.ActiveSolutionProjects;
            return projects.Cast<Project>().Where(IsCppProject).Select(p => { Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread(); return p.Object; }).OfType<VCProject>().FirstOrDefault();
        }

        public async Task<ConanProject> ExtractConanProjectAsync(VCProject vcProject)
        {
            var projectPath = await ConanPathHelper.GetNearestConanfilePath(vcProject.ProjectDirectory);
            if (projectPath == null)
            {
                return null;
            }
            var project = new ConanProject
            {
                Path = projectPath,
                InstallPath = Path.Combine(projectPath, ".conan")
            };

            foreach (VCConfiguration configuration in vcProject.Configurations)
            {
                project.Configurations.Add(ExtractConanConfiguration(configuration));
            }

            return project;
        }

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

        private static ConanConfiguration ExtractConanConfiguration(VCConfiguration configuration)
        {
            var x = configuration.Platform;
            IVCRulePropertyStorage generalSettings = configuration.Rules.Item("ConfigurationGeneral");
            var toolset = generalSettings.GetEvaluatedPropertyValue("PlatformToolset");
            return new ConanConfiguration
            {
                Architecture = GetArchitecture(configuration.Platform.Name),
                BuildType = GetBuildType(configuration.ConfigurationName),
                CompilerToolset = toolset,
                CompilerVersion = "15"
            };
        }

        public Task AddPropsImportAsync(string projectPath, string propFilePath) => Task.Run(() =>
        {
            var xml = XDocument.Load(projectPath);
            var project = xml.Root;
            if (project == null)
            {
                throw new Exception($"Project {projectPath} is malformed: no root element");
            }

            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            var import = ns + "Import";
            var existingImports = project.Descendants(import);
            if (existingImports.Any(node => node.Attribute("Project")?.Value == propFilePath))
            {
                return;
            }

            var newImport = new XElement(import);
            newImport.SetAttributeValue("Project", propFilePath);
            newImport.SetAttributeValue("Condition", $"Exists('{propFilePath}')");
            project.Add(newImport);

            xml.Save(projectPath);
        });

        public Guid UnloadProject(VCProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projectGuid = new Guid(project.ProjectGUID);
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

            int hr = solution.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
            ErrorHandler.ThrowOnFailure(hr);

            return projectGuid;
        }

        public void ReloadProject(Guid projectGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution4;

            int hr = solution.ReloadProject(ref projectGuid);
            ErrorHandler.ThrowOnFailure(hr);
        }
    }
}
