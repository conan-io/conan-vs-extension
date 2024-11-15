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
            ThreadHelper.ThrowIfNotOnUIThread();
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
    if errorlevel 1 (
        set performInstall=1
        echo Changes detected in conandata.yml
    )
)

if exist "".conan\CONANFILE_%CONAN_BUILD_CONFIG%"" (
    echo Checking changes in conanfile.py
    fc ""conanfile.py"" "".conan\CONANFILE_%CONAN_BUILD_CONFIG%"" > nul
    if errorlevel 1 (
        set performInstall=1
        echo Changes detected in conanfile.py
    )
)

REM Check for the .runconan file that indicates changes in profiles

if exist "".conan\.runconan"" (
    echo Changes in profiles detected
    set performInstall=1
    del "".conan\.runconan""
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

    if !errorlevel! neq 0 (
        echo ERROR: Conan installation failed. Please check the console output to troubleshoot issues.
        exit /b 1
    )

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

        public static async Task SaveConanPrebuildEventsAllConfigAsync(Project project)
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

            // Access startup projects from SolutionBuild
            var startupProjects = dte.Solution.SolutionBuild.StartupProjects as object[];

            if (startupProjects != null && startupProjects.Length > 0)
            {
                // Get the name of the first startup project
                string startupProjectName = (string)startupProjects[0];

                // Iterate through all projects in the solution to find a matching project
                foreach (Project project in dte.Solution.Projects)
                {
                    Project result = FindProjectByNameRecursive(project, startupProjectName);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null; // No startup project found
        }

        // Helper function to find the project by name recursively
        private static Project FindProjectByNameRecursive(Project project, string projectName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (project.UniqueName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }

            // Handle solution folders
            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        Project foundProject = FindProjectByNameRecursive(item.SubProject, projectName);
                        if (foundProject != null)
                        {
                            return foundProject;
                        }
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
                Project result = FindProjectByNameRecursive(project, name);

                if (result != null)
                {
                    return result;
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
