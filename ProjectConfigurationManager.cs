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

        static private bool conandataFileExists(Project project)
        {
            string projectDirectory = System.IO.Path.GetDirectoryName(project.FullName);
            string path = Path.Combine(projectDirectory, "conandata.yml");
            return File.Exists(path);
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

            if (!conandataFileExists(project))
            {
                System.Diagnostics.Debug.WriteLine("conandata.yml not found. Skipping Conan Deps.");
                return;
            }

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

REM Initialize flags

REM Check if the control files exist

set performInstall=0

if not exist "".conan\CONANDATA_%CONAN_BUILD_CONFIG%"" set ""performInstall=1""
if not exist "".conan\CONANFILE_%CONAN_BUILD_CONFIG%"" set ""performInstall=1""

REM Check for changes in conandata.yml and conanfile.py

if exist "".conan\CONANDATA_%CONAN_BUILD_CONFIG%"" (
    echo Checking changes in conandata.yml
    fc ""conandata.yml"" "".conan\CONANDATA_%CONAN_BUILD_CONFIG%"" > nul
    if %errorlevel% equ 1 set performInstall=1
)

if exist "".conan\CONANFILE_%CONAN_BUILD_CONFIG%"" (
    echo Checking changes in conanfile.py
    fc ""conanfile.py"" "".conan\CONANFILE_%CONAN_BUILD_CONFIG%"" > nul
    if %errorlevel% equ 1 set performInstall=1
)

if %performInstall% equ 1 (
    REM Echo changes detected
    echo Changes detected, executing conan install...
    
    set ""args=""

    :args_loop
    if ""%~1""=="""" goto after_args_loop
    set ""args=!args! %1""
    shift
    goto args_loop

    :after_args_loop
    echo Arguments for conan install: !args!

    ""{conanPath}"" install !args!

    REM Update control files to reflect current state
    copy /Y conandata.yml .conan\CONANDATA_%CONAN_BUILD_CONFIG%
    copy /Y conanfile.py .conan\CONANFILE_%CONAN_BUILD_CONFIG%

    REM Display the message indicating Conan install finished
    echo ****************************************************************
    echo *                                                              *
    echo *   Conan installation completed successfully.                 *
    echo *   Please relaunch the build to apply the new changes.        *
    echo *                                                              *
    echo ****************************************************************
    echo ERROR: Conan installation completed successfully. Please relaunch the build to apply the new changes.
    exit /b 1
)

REM Echo no changes detected
echo No changes detected, skipping conan install...
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
                string commandLine = $"set CONAN_BUILD_CONFIG=\"$(Configuration)_$(Platform)\" && \"$(ProjectDir).conan\\{conan_script_name}\" . -pr:h=.conan/$(Configuration)_$(Platform) -pr:b=default --build=missing";
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

            if (!conandataFileExists(project))
            {
                System.Diagnostics.Debug.WriteLine("conandata.yml not found. Skipping Conan PreBuildEvent.");
                return;
            }

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
