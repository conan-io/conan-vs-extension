using System;
using System.ComponentModel.Design;
using Conan.VisualStudio.Services;

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

        protected internal abstract System.Threading.Tasks.Task MenuItemCallback();

        private void InitializeMenuItem(IMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(CommandSetId, CommandId);
            var menuItem = new MenuCommand(async (_, __) =>
            {
                try
                {
                    await MenuItemCallback();
                }
                catch (Exception exception)
                {
                    _dialogService.ShowPluginError(exception.ToString());
                }
            }, menuCommandId);
            commandService.AddCommand(menuItem);
        }
    }
}
