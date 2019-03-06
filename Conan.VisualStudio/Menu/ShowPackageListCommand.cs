using System;
using System.ComponentModel.Design;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    internal sealed class ShowPackageListCommand : MenuCommandBase
    {
        protected override int CommandId => 4129;

        private readonly Package _package;

        public ShowPackageListCommand(Package package, IMenuCommandService commandService, IDialogService dialogService)
            : base(commandService, dialogService) => _package = package;

        protected internal override async Task MenuItemCallbackAsync()
        {
            var window = _package.FindToolWindow(typeof(PackageListToolWindow), 0, create: true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ShowWindowFrame(window);

            await TaskScheduler.Default;
        }

        private static void ShowWindowFrame(ToolWindowPane window)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
