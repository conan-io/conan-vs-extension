using System;
using System.ComponentModel.Design;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Base class for a menu command.</summary>
    internal abstract class MenuCommandBase
    {
        private static readonly Guid CommandSetId = new Guid("614d6e2d-166a-4d8c-b047-1c2248bbef97");

        private readonly IDialogService _dialogService;

        protected abstract int CommandId { get; }

        public MenuCommandBase(IMenuCommandService commandService, IDialogService dialogService)
        {
            _dialogService = dialogService;
            InitializeMenuItem(commandService);
        }

        protected internal abstract System.Threading.Tasks.Task MenuItemCallbackAsync();

        async System.Threading.Tasks.Task CallMenuItemBallbackAsync()
        {
            try
            {
                await MenuItemCallbackAsync();
            }
            catch (Exception exception)
            {
                _dialogService.ShowPluginError(exception.ToString());
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(
                async delegate
                {
                    await CallMenuItemBallbackAsync();
                }
            );
        }

        private void InitializeMenuItem(IMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(CommandSetId, CommandId);
            var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
            commandService.AddCommand(menuItem);
        }
    }
}
