using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

namespace VSConanPackage
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddConanDepends
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("614d6e2d-166a-4d8c-b047-1c2248bbef97");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddConanDepends"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private AddConanDepends(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AddConanDepends Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new AddConanDepends(package);
        }

        internal static VCProject GetActiveProject()
        {
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;

            return GetActiveProject(dte);
        }

        internal static VCProject GetActiveProject(DTE dte)
        {
            var active_projects = dte.ActiveSolutionProjects as Array;
            if (active_projects == null || active_projects.Length == 0)
                return null;
            for (var i = 0; i < active_projects.Length; ++i)
            {
                var project = active_projects.GetValue(i) as Project;
                var shim = project.Object;
                if (IsCppProject(project))
                    return shim as VCProject;
            }
            return null;
           //return dte.Solution.Projects.Item(1).Object as VCProject;
            //if (!(dte.ActiveSolutionProjects is Array activeSolutionProjects) || activeSolutionProjects.Length == 0)
            //    return null;

            //return activeSolutionProjects.GetValue(0) as VCProject;
        }

        private static bool IsCppProject(Project project)
        {
            return project != null
                   && (project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageMC
                       || project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageVC);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            VCProject vcProject = GetActiveProject();
            if (vcProject == null)//!IsCppProject(project))
            {
                ErrorMessageBox("A C++ project with a conan file must be selected.");
                return;
            }
           // var vcProject = project as VCProject;
            
            

            if (VsShellUtilities.ShowMessageBox(this.ServiceProvider,
                    string.Format("Process conanbuild.txt for '{0}'?\n", vcProject.Name),
                    string.Empty,
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST) == DialogResult.Cancel)
                return;

            // I think you will like the visual_studio_multi generator that has been contributed to the source code, 
            // is already merged to develop and will be in next conan 0.28 release: #1831

            // It automates further the creation and loading of the conanbuildinfo.props for all 4 configurations.

            // conan install -s .... commands are invoked to download the deps if they are not yet installed on the system

            // include property files in project for each configuration

            // -As some kind of "pre-build" task:
            //---Create a unique hidden temp directory under ".vs"
            // CC: call it conan_packages
            //  subdirs unique?
            //var tempDir = VCConfiguration.Evaluate("$(IntDir)\\ConanDeps");
            var tempDir = System.IO.Path.GetTempPath();
            tempDir = System.IO.Path.Combine(tempDir, "conan_deps", vcProject.Name);
            var dirInfo = System.IO.Directory.CreateDirectory(tempDir);
            //    ------Use the current project settings like release - x64 for the name
            //---Run the "conan install . " with the output going to the temp directory
            //-- - Load the generated props file into the active project

            // should probably figure out the configurations
            //var archs = vcProject.ConfigurationManager.PlatformNames as Array;
            //var buildTypes = project.ConfigurationManager.ConfigurationRowNames as Array;
            //if (archs is null || buildTypes is null)
            //{
            //    ErrorMessageBox("No valid build definitions in configuration manager.");
            //    return;
            //}

            foreach (var cfg in vcProject.Configurations)
            {
                var tools = cfg.Tools as IVCCollection;
                var tool = tools.Item("VCCLCompilerTool") as VCCLCompilerTool;

                // var tool = cfg.Tools("VCCLCompilerTool");
                string runTime = "MT";
                switch (tool.RuntimeLibrary)
                {
                    case runtimeLibraryOption.rtMultiThreadedDLL:
                        runTime = "MD";
                        break;
                    case runtimeLibraryOption.rtMultiThreadedDebug:
                        runTime = "MTd";
                        break;
                    case runtimeLibraryOption.rtMultiThreadedDebugDLL:
                        runTime = "MDd";
                        break;
                    default:
                            runTime = "MT";
                            break;
                }

                var platform = cfg.Platform.Name; // hopefully just x86 or x64
                var cfgName = cfg.ConfigurationName; // hopefully Debug or Release

                string args = $"install . -g visual_studio_multi -s arch={platform} -s build_type={cfgName} -s compiler=\"Visual Studio\" -s compiler.version=14 -s compiler.runtime={runTime} --build missing --update";

                RunConan(args);
            }
            


        }

        private void RunConan(string args)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "conan",
                    Arguments = $"{args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
       
            using (var reader = process.StandardOutput)
            {
                var result = reader.ReadToEnd();
                Console.Write(result);
            }
            
        }

        private void ErrorMessageBox(string errorMessage)
        {
            VsShellUtilities.ShowMessageBox(this.ServiceProvider,
                errorMessage,
                string.Empty,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
