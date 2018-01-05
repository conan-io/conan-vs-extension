using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Conan.VisualStudio.Menu;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    /// <summary>This is the class that implements the package exposed by this assembly.</summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(VSConanPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(ConanOptionsPage), "Conan", "Main", 0, 0, true)]
    [ProvideToolWindow(typeof(PackageListToolWindow))]
    public sealed class VSConanPackage : Package
    {
        /// <summary>
        /// VSConanPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "33315c89-72dd-43bb-863c-561c1aa5ed54";

        private AddConanDepends _addConanDepends;
        private ShowPackageListCommand _showPackageListCommand;
        private IntegrateIntoProjectCommand _integrateIntoProjectCommand;

        /// <summary>Initialization of the package; this method is called right after the package is sited.</summary>
        protected override void Initialize()
        {
            base.Initialize();

            var dialogService = new VisualStudioDialogService(this);
            var commandService = GetService<IMenuCommandService>();
            var projectService = new VcProjectService();
            var settingsService = new VisualStudioSettingsService(this);

            _addConanDepends = new AddConanDepends(commandService, dialogService, projectService, settingsService);
            _showPackageListCommand = new ShowPackageListCommand(this, commandService, dialogService);
            _integrateIntoProjectCommand = new IntegrateIntoProjectCommand(commandService, dialogService, projectService);
        }

        private T GetService<T>() where T : class =>
            (T)GetService(typeof(T)) ?? throw new Exception($"Cannot initialize service {typeof(T).FullName}");
    }
}
