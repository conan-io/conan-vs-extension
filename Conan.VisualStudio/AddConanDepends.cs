using System;
using System.ComponentModel.Design;
using System.IO;
using Conan.VisualStudio.Core;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio
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
        private async void MenuItemCallback(object sender, EventArgs e)
        {
            VCProject vcProject = GetActiveProject();
            if (vcProject == null)//!IsCppProject(project))
            {
                ErrorMessageBox("A C++ project with a conan file must be selected.");
                return;
            }

            if (VsShellUtilities.ShowMessageBox(this.ServiceProvider,
                    string.Format("Process conanbuild.txt for '{0}'?\n", vcProject.Name),
                    string.Empty,
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST) == DialogResult.Cancel)
                return;

            var conanPath = package.GetConanExecutablePath();
            if (conanPath == null)
            {
                ErrorMessageBox(
                    "Conan executable path is not set and Conan executable wasn't found automatically. " +
                    "Please set it up in the Tools → Settings → Conan menu.");
                return;
            }

            var conan = new ConanRunner(conanPath);
            var project = VcProjectService.ExtractConanConfiguration(vcProject);
            await InstallDependencies(conan, project);
        }

        private async Task InstallDependencies(ConanRunner conan, ConanProject project)
        {
            try
            {
                var process = await conan.Install(project);
                using (var reader = process.StandardOutput)
                {
                    var result = reader.ReadToEnd();
                    Console.Write(result);
                }

                process.WaitForExit();
            }
            catch (FileNotFoundException)
            {
                ErrorMessageBox("Could not locate conan on execution path.");
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
