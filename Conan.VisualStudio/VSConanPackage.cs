using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Conan.VisualStudio
{
    /// <summary>This is the class that implements the package exposed by this assembly.</summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Indicate we want to load whenever VS opens, so that we can hopefully catch the Solution_Opened event
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(ConanOptionsPage), "Conan", "Main", 0, 0, true)]
    [ProvideToolWindow(typeof(PackageListToolWindow))]
    public sealed class VSConanPackage : AsyncPackage
    {
        /// <summary>
        /// VSConanPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "33315c89-72dd-43bb-863c-561c1aa5ed54";

        private AddConanDepends _addConanDepends;
        private ShowPackageListCommand _showPackageListCommand;
        private IntegrateIntoProjectCommand _integrateIntoProjectCommand;
        private DTE _dte;
        private SolutionEvents _solutionEvents;
        private IVsSolution _solution;
        private SolutionEventsHandler _solutionEventsHandler;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            _dte = await GetServiceAsync<DTE>();
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);
            var dialogService = new VisualStudioDialogService(serviceProvider);
            var commandService = await GetServiceAsync<IMenuCommandService>();
            var projectService = new VcProjectService();
            var settingsService = new VisualStudioSettingsService(this);

            _solution = await GetServiceAsync<SVsSolution>() as IVsSolution;
            _solutionEventsHandler = new SolutionEventsHandler(this);
            _solution.AdviseSolutionEvents(_solutionEventsHandler, out var _solutionEventsCookie);

            _addConanDepends = new AddConanDepends(commandService, dialogService, projectService, settingsService, serviceProvider);
            _showPackageListCommand = new ShowPackageListCommand(this, commandService, dialogService);
            _integrateIntoProjectCommand = new IntegrateIntoProjectCommand(commandService, dialogService, projectService);

            Logger.Initialize(serviceProvider, "Conan");

            SubscribeToEvents();
        }

        private async Task<T> GetServiceAsync<T>() where T : class =>
            await GetServiceAsync(typeof(T)) as T ?? throw new Exception($"Cannot initialize service {typeof(T).FullName}");

        /// <summary>
        /// Use the DTE object to gain access to Solution events
        /// </summary>
        private void SubscribeToEvents()
        {
            /**
             * Note that _solutionEvents is not a local variable but a class variable
             * to prevent from Visual Studio garbage collecting our variable which would
             * mean missed events.
            **/
            _solutionEvents = _dte.Events.SolutionEvents;

            /**
             * SolutionEvents_Opened should give us an event for opened solutions and projects
             * according to https://docs.microsoft.com/en-us/dotnet/api/envdte.solutioneventsclass.opened?view=visualstudiosdk-2017
             */
            _solutionEvents.Opened += SolutionEvents_Opened;
        }

        /// <summary>
        /// Handler to react on a solution opened event
        /// </summary>
        private void SolutionEvents_Opened()
        {
            /**
             * Get all projects within the solution
             */
            var projects = _dte.Solution.Projects;

            /**
             * For each project call Conan
             */
            foreach (Project project in projects)
            {
                /*
                 * This would be the place to start reading the project-specific JSON file
                 * to determine what command to run. At this stage we have a <see cref="Project"/> object
                 * of which we can use the FileName property to use in the command.
                 */
                var fileName = project.FileName;
            }
        }
    }
}
