using System;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Conan.VisualStudio.Services
{
    internal class VisualStudioDialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public VisualStudioDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ShowInfo(string text) =>
            ShowMessage(
                text,
                string.Empty,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK);

        public bool ShowOkCancel(string text) =>
            ShowMessage(
                text,
                string.Empty,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL) == DialogResult.OK;

        public void ShowPluginError(string error) =>
            ShowMessage(
                error,
                "Conan Visual Studio Plugin Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK);

        private int ShowMessage(string text, string title, OLEMSGICON icon, OLEMSGBUTTON button) =>
            VsShellUtilities.ShowMessageBox(
                _serviceProvider,
                text,
                title,
                icon,
                button,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }
}
