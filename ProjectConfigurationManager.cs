using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Windows.Forms;
using VSLangProj;

namespace conan_vs_extension
{
    public class ProjectConfigurationManager
    {
        
        public ProjectConfigurationManager()
        {
        }

        public static async Task InjectConanDepsToAllConfigsAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (project.Object is VCProject vcProject)
            {
                string propsFilePath = GetPropsFilePath(project);
                if (File.Exists(propsFilePath))
                {
                    foreach (VCConfiguration vcConfig in (IEnumerable)vcProject.Configurations)
                    {
                        InjectConanDepsToConfig(vcConfig, propsFilePath);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Properties file '{propsFilePath}' does not exist.");
                }
            }
        }

        public static async Task InjectConanDepsAsync(Project project, VCConfiguration vcConfig)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string propsFilePath = GetPropsFilePath(project);
            if (File.Exists(propsFilePath))
            {
                InjectConanDepsToConfig(vcConfig, propsFilePath);
                project.Save();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Properties file '{propsFilePath}' does not exist.");
            }
        }

        private static void InjectConanDepsToConfig(VCConfiguration vcConfig, string propsFilePath)
        {
            bool isAlreadyIncluded = false;
            IVCCollection propertySheets = vcConfig.PropertySheets as IVCCollection;
            foreach (VCPropertySheet sheet in propertySheets)
            {
                if (sheet.PropertySheetFile.Equals(propsFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    isAlreadyIncluded = true;
                    break;
                }
            }
            if (!isAlreadyIncluded)
            {
                vcConfig.AddPropertySheet(propsFilePath);
            }
        }
        
        private static string GetPropsFilePath(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string projectFilePath = project.FullName;
            string projectDirectory = Path.GetDirectoryName(projectFilePath);
            return Path.Combine(projectDirectory, "conan", "conandeps.props");
        }

        private static async Task SaveConanPrebuildEventAsync(Project project, VCConfiguration vcConfig, string conanCommand)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VCProject vcProject = project.Object as VCProject;
            IVCCollection tools = (IVCCollection)vcConfig.Tools;
            VCPreBuildEventTool preBuildTool = (VCPreBuildEventTool)tools.Item("VCPreBuildEventTool");

            if (preBuildTool != null)
            {
                string currentPreBuildEvent = preBuildTool.CommandLine;
                if (!currentPreBuildEvent.Contains("conan"))
                {
                    // FIXME: better do this with a script file?
                    preBuildTool.CommandLine = conanCommand + Environment.NewLine + currentPreBuildEvent;
                    vcProject.Save();
                }
            }
        }

        public static void SaveConanPrebuildEventsAllConfig(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string conanPath = GlobalSettings.ConanExecutablePath;
            VCProject vcProject = project.Object as VCProject;
            foreach (VCConfiguration vcConfig in (IEnumerable)vcProject.Configurations)
            {
                string profileName = ConanProfilesManager.getProfileName(vcConfig);
                string prebuildCommand = $"\"{conanPath}\" install . -pr:h=.conan/{profileName} --build=missing";
                _ = SaveConanPrebuildEventAsync(project, vcConfig, prebuildCommand);
            }

        }

        public static VCConfiguration GetActiveVCConfiguration(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (project.Object is VCProject vcProject)
            {
                IVCCollection configurations = vcProject.Configurations as IVCCollection;
                VCConfiguration activeConfiguration = null;

                foreach (VCConfiguration config in configurations)
                {
                    string configName = config.ConfigurationName;
                    VCPlatform vcPlatform = config.Platform as VCPlatform;
                    if (config.ConfigurationName == project.ConfigurationManager.ActiveConfiguration.ConfigurationName &&
                        vcPlatform.Name == project.ConfigurationManager.ActiveConfiguration.PlatformName)
                    {
                        activeConfiguration = config;
                        break;
                    }
                }

                return activeConfiguration;
            }

            return null;
        }

        public static Project GetStartupProject(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SolutionBuild solutionBuild = dte.Solution.SolutionBuild;
            string startupProjectName = (string)((Array)solutionBuild.StartupProjects).GetValue(0);
            foreach (Project project in dte.Solution.Projects)
            {
                if (project.UniqueName == startupProjectName)
                {
                    return project;
                }
            }

            return null;
        }

        public static Project GetProjectByName(DTE dte, string name)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (Project project in dte.Solution.Projects)
            {
                if (project.UniqueName == name)
                {
                    return project;
                }
            }

            return null;
        }

        public static VCConfiguration GetVCConfig(Project project, string ProjectConfig, string Platform)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (project.Object is VCProject vcProject)
            {
                foreach (VCConfiguration vcConfig in (IEnumerable)vcProject.Configurations)
                {
                    VCPlatform vcPlatform = vcConfig.Platform as VCPlatform;
                    if (vcConfig.ConfigurationName.Equals(ProjectConfig) 
                        && vcPlatform.Name.Equals(Platform)) { 
                        return vcConfig; 
                    }  
                }
            }
            return null;
        }
    }
}
