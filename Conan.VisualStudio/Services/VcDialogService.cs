using System;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Conan.VisualStudio.Services
{
    internal class VcDialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public VcDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool ShowOkCancel(string text) =>
            VsShellUtilities.ShowMessageBox(
                _serviceProvider,
                text,
                string.Empty,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST) == DialogResult.OK;

        public void ShowPluginError(string error) =>
            VsShellUtilities.ShowMessageBox(
                _serviceProvider,
                error,
                "Conan Visual Studio Plugin Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }
}
