using System;
using System.ComponentModel.Design;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    internal sealed class ShowPackageListCommand : MenuCommandBase
    {
        protected override int CommandId => 4129;

        private readonly Package _package;

        public ShowPackageListCommand(Package package, IMenuCommandService commandService, IDialogService dialogService)
            : base(commandService, dialogService) => _package = package;

        protected internal override Task MenuItemCallback()
        {
            var window = _package.FindToolWindow(typeof(PackageListToolWindow), 0, create: true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

            return Task.CompletedTask;
        }
    }
}
