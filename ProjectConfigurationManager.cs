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

        private static async Task GenerateConanInstallScriptAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string conan_script_name = "conan_install.bat";
            string projectDirectory = Path.GetDirectoryName(project.FullName);
            string conanScriptDirectory = Path.Combine(projectDirectory, ".conan");
            string scriptPath = Path.Combine(conanScriptDirectory, conan_script_name);
            string conanPath = GlobalSettings.ConanExecutablePath;

            string conanCommandContents = $@"@echo off
setlocal enabledelayedexpansion

set ""args=""

:args_loop
if ""%~1""=="""" goto after_args_loop
set ""args=!args! %1""
shift
goto args_loop

:after_args_loop
echo Arguments for conan install: !args!

""{conanPath}"" install !args!
        ";

            Directory.CreateDirectory(conanScriptDirectory);
            File.WriteAllText(scriptPath, conanCommandContents);
        }

        private static async Task SaveConanPrebuildEventAsync(Project project, VCConfiguration vcConfig)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VCProject vcProject = project.Object as VCProject;
            IVCCollection tools = vcConfig.Tools as IVCCollection;
            VCPreBuildEventTool preBuildTool = tools.Item("VCPreBuildEventTool") as VCPreBuildEventTool;

            if (preBuildTool != null)
            {
                string conan_script_name = "conan_install.bat";
                string commandLine = $"\"$(ProjectDir).conan\\{conan_script_name}\" . -pr:h=.conan/$(Configuration)_$(Platform) -pr:b=default --build=missing";
                if (!preBuildTool.CommandLine.Contains(conan_script_name))
                {
                    preBuildTool.CommandLine += Environment.NewLine + commandLine;
                    vcProject.Save();
                }
            }
        }

        public static async void SaveConanPrebuildEventsAllConfig(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await GenerateConanInstallScriptAsync(project); // all the config share the same script

            VCProject vcProject = project.Object as VCProject;
            foreach (VCConfiguration vcConfig in (IEnumerable)vcProject.Configurations)
            {
                await SaveConanPrebuildEventAsync(project, vcConfig);
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
            if (solutionBuild.StartupProjects != null)
            {
                string startupProjectName = (string)((Array)solutionBuild.StartupProjects).GetValue(0);
                foreach (Project project in dte.Solution.Projects)
                {
                    if (project.UniqueName == startupProjectName)
                    {
                        return project;
                    }
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
