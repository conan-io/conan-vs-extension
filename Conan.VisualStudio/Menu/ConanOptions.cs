using System.ComponentModel.Design;
using Conan.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Conan.VisualStudio.Menu
{
    /// <summary>Command handler.</summary>
    internal sealed class ConanOptions : MenuCommandBase
    {
        protected override int CommandId => 0x0103;

        private readonly OptionsDelegate _optionsDelegate;

        public delegate void OptionsDelegate();

        public ConanOptions(
            IMenuCommandService commandService,
            IDialogService dialogService,
            OptionsDelegate optionsDelegate)
            : base(commandService, dialogService)
        {
            _optionsDelegate = optionsDelegate;
        }

        protected internal override async Task MenuItemCallbackAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _optionsDelegate();
        }
    }
}
