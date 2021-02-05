using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Conan.VisualStudio.Core.VCInterfaces;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Conan.VisualStudio
{
    /// <summary>This is the class that implements the package exposed by this assembly.</summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuids.guidVSConanPackageString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Indicate we want to load whenever VS opens, so that we can hopefully catch the Solution_Opened event
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionHasMultipleProjects, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionHasSingleProject, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.EmptySolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(ConanOptionsPage), "Conan", "Main", 0, 0, true)]
    [ProvideAppCommandLine(_cliSwitch, typeof(VSConanPackage), Arguments = "0", DemandLoad = 1, PackageGuid = PackageGuids.guidVSConanPackageString)]
    public sealed class VSConanPackage : AsyncPackage, IVsUpdateSolutionEvents3
    {
        private const string _cliSwitch = "ConanVisualStudioVersion";
        private AddConanDependsProject _addConanDependsProject;
        private AddConanDependsSolution _addConanDependsSolution;
        private ConanOptions _conanOptions;
        private ConanAbout _conanAbout;
        private DTE _dte;
        private SolutionEvents _solutionEvents;
        private IVsSolution4 _solution;
        private ISettingsService _settingsService;
        private IVcProjectService _vcProjectService;
        private IConanService _conanService;
        private IVsSolutionBuildManager3 _solutionBuildManager;
        private ProjectItemsEvents _projectItemEvents;
        private DocumentEvents _documentEvents;
        private IErrorListService _errorListService;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Handle commandline switch
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cmdLine = await GetServiceAsync(typeof(SVsAppCommandLine)) as IVsAppCommandLine;
            ErrorHandler.ThrowOnFailure(cmdLine.GetOption(_cliSwitch, out int isPresent, out string optionValue));
            if (isPresent == 1)
            {
                System.Console.WriteLine(Vsix.Version);
            }

            _dte = await GetServiceAsync<DTE>();

            _solution = await GetServiceAsync<SVsSolution>() as IVsSolution4;
            _solutionBuildManager = await GetServiceAsync<IVsSolutionBuildManager>() as IVsSolutionBuildManager3;

            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)_dte);

            await TaskScheduler.Default;

            var commandService = await GetServiceAsync<IMenuCommandService>();
            _vcProjectService = new VcProjectService();
            _settingsService = new VisualStudioSettingsService(this);
            _errorListService = new ErrorListService();
            _conanService = new ConanService(_settingsService, _errorListService, _vcProjectService, _solution, _dte);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _addConanDependsProject = new AddConanDependsProject(commandService, _errorListService, _vcProjectService, _conanService);
            _addConanDependsSolution = new AddConanDependsSolution(commandService, _errorListService, _vcProjectService,  _conanService);

            _conanOptions = new ConanOptions(commandService, _errorListService, ShowOptionPage);
            _conanAbout = new ConanAbout(commandService, _errorListService);

            await TaskScheduler.Default;

            Logger.Initialize(serviceProvider, "Conan");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            SubscribeToEvents();

            EnableMenus(_dte.Solution != null && _dte.Solution.IsOpen);

            await TaskScheduler.Default;
        }

        private void ShowOptionPage()
        {
            base.ShowOptionPage(typeof(ConanOptionsPage));
        }

        private void EnableMenus(bool enable)
        {
            _addConanDependsProject.EnableMenu(enable);
            _addConanDependsSolution.EnableMenu(enable);
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

            ThreadHelper.ThrowIfNotOnUIThread();

            _solutionEvents = _dte.Events.SolutionEvents;

            /**
             * SolutionEvents_Opened should give us an event for opened solutions and projects
             * according to https://docs.microsoft.com/en-us/dotnet/api/envdte.solutioneventsclass.opened?view=visualstudiosdk-2017
             */
            _solutionEvents.Opened += SolutionEvents_Opened;
            _solutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            _solutionEvents.ProjectAdded += SolutionEvents_ProjectAdded;

            _projectItemEvents = (_dte.Events as EnvDTE80.Events2).ProjectItemsEvents;
            _projectItemEvents.ItemAdded += SolutionItemEvents_ItemAdded;
            _projectItemEvents.ItemRenamed += SolutionItemEvents_ItemRenamed;

            _documentEvents = _dte.Events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

            if (_solutionBuildManager != null)
                _solutionBuildManager.AdviseUpdateSolutionEvents3(this, out uint pdwcookie);
        }

        public static bool IsConanfile(string name)
        {
            return (name.ToLower() == "conanfile.txt" || name.ToLower() == "conanfile.py");
        }

        public void SolutionItemEvents_ItemAdded(ProjectItem ProjectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsConanfile(ProjectItem.Name))
                InstallConanDepsIfRequired(ProjectItem.ContainingProject);
        }

        public void SolutionItemEvents_ItemRenamed(ProjectItem ProjectItem, string OldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsConanfile(ProjectItem.Name))
                InstallConanDepsIfRequired(ProjectItem.ContainingProject);
        }

        public void DocumentEvents_DocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsConanfile(document.ProjectItem.Name))
                InstallConanDepsIfRequired(document.ProjectItem.ContainingProject);
        }

        public int OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_settingsService.GetConanInstallOnlyActiveConfiguration())
                InstallConanDepsIfRequired();
            return VSConstants.S_OK;
        }

        private void InstallConanDeps(IVCProject vcProject)
        {
            _errorListService.Clear();
            ThreadHelper.JoinableTaskFactory.RunAsync(
                async delegate
                {
                    bool success = await _conanService.InstallAsync(vcProject);
                    if (success)
                    {
                        await _conanService.IntegrateAsync(vcProject);
                    }
                }
            );
        }

        private void SolutionEvents_AfterClosing()
        {
            EnableMenus(false);
        }

        private void SolutionEvents_ProjectAdded(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_conanService.RefreshingProjects.Contains(project.FullName))
                _conanService.RefreshingProjects.Remove(project.FullName);
            else
                InstallConanDepsIfRequired(project);
        }

        private void InstallConanDepsIfRequired(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_settingsService.GetConanInstallAutomatically())
            {
                if (_vcProjectService.IsConanProject(project))
                    InstallConanDeps(_vcProjectService.AsVCProject(project));
            }
        }

        private void InstallConanDepsIfRequired()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_settingsService.GetConanInstallAutomatically())
            {
                foreach (Project project in _dte.Solution.Projects)
                {
                    if (_vcProjectService.IsConanProject(project))
                        InstallConanDeps(_vcProjectService.AsVCProject(project));
                }
            }
        }

        /// <summary>
        /// Handler to react on a solution opened event
        /// </summary>
        private void SolutionEvents_Opened()
        {
            /**
             * Get all projects within the solution
             */
            ThreadHelper.ThrowIfNotOnUIThread();

            EnableMenus(true);

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
            InstallConanDepsIfRequired();
        }
    }
}
