using System;
using System.ComponentModel.Design;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Base class for a menu command.</summary>
    internal abstract class MenuCommandBase
    {
        private readonly Core.IErrorListService _errorListService;
        private readonly OleMenuCommand _menuCommand;

        protected abstract int CommandId { get; }

        public MenuCommandBase(IMenuCommandService commandService, Core.IErrorListService errorListService)
        {
            _errorListService = errorListService;
            var menuCommandId = new CommandID(PackageGuids.guidVSConanPackageCmdSet, CommandId);
            _menuCommand = new OleMenuCommand(MenuItemCallback, menuCommandId);
            _menuCommand.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
            commandService.AddCommand(_menuCommand);
        }

        protected internal abstract System.Threading.Tasks.Task MenuItemCallbackAsync();

        async System.Threading.Tasks.Task CallMenuItemBallbackAsync()
        {
            try
            {
                await MenuItemCallbackAsync().ConfigureAwait(true);
            }
            catch (NotSupportedException exception)
            {
                _errorListService.WriteError(exception.ToString());
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(
                async delegate
                {
                    await CallMenuItemBallbackAsync().ConfigureAwait(true);
                }
            );
        }

        public void EnableMenu(bool enable)
        {
            _menuCommand.Enabled = enable;
        }

        protected virtual void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            // do nothing, override in child, if necessary
        }
    }
}
