using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio.Services
{
    internal class VisualStudioSettingsService : ISettingsService
    {
        private readonly Package _package;

        public VisualStudioSettingsService(Package package)
        {
            _package = package;
        }

        public string GetConanExecutablePath()
        {
            // Believe it or not, it's the recommended way of retrieving the option value per
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/creating-an-options-page#accessing-options
            var page = (ConanOptionsPage)_package.GetDialogPage(typeof(ConanOptionsPage));
            return page.ConanExecutablePath;
        }
    }
}
