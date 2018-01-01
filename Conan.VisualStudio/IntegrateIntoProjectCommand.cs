using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    /// <summary>
    /// Command to install the Conan prop files into the project file.
    /// </summary>
    internal sealed class IntegrateIntoProjectCommand
    {
        public const int CommandId = 4130;
        public static readonly Guid CommandSet = new Guid("614d6e2d-166a-4d8c-b047-1c2248bbef97");

        /// <summary>
        /// VS Package that provides this command.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrateIntoProjectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package.</param>
        private IntegrateIntoProjectCommand(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static IntegrateIntoProjectCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package.</param>
        public static void Initialize(Package package)
        {
            Instance = new IntegrateIntoProjectCommand(package);
        }

        private async void MenuItemCallback(object sender, EventArgs e)
        {
            var project = VcProjectService.GetActiveProject();
            await VcProjectService.AddPropsImport(project.ProjectFile, @"conan\conanbuildinfo_multi.props");
        }
    }
}
