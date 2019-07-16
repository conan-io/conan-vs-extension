using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.IO;

namespace Conan.VisualStudio.Services
{
    public class VisualStudioSettingsService : ISettingsService
    {
        private readonly Package _package;

        public VisualStudioSettingsService(Package package)
        {
            _package = package;
        }

        private ConanOptionsPage GetConanPage()
        {
            // Believe it or not, it's the recommended way of retrieving the option value per
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/creating-an-options-page#accessing-options
            return (ConanOptionsPage)_package.GetDialogPage(typeof(ConanOptionsPage));
        }

        public string GetConanExecutablePath()
        {
            return GetConanPage().ConanExecutablePath;
        }

        public string GetConanInstallationPath()
        {
            return GetConanPage().ConanInstallationPath;
        }

        public bool GetConanInstallOnlyActiveConfiguration()
        {
            return GetConanPage().ConanInstallOnlyActiveConfiguration;
        }

        public ConanGeneratorType GetConanGenerator()
        {
            return GetConanPage().ConanGenerator;
        }

        public bool GetConanInstallAutomatically()
        {
            return GetConanPage().ConanInstallAutomatically;
        }

        public ConanBuildType GetConanBuild()
        {
            return GetConanPage().ConanBuild;
        }

        public bool GetConanUpdate()
        {
            return GetConanPage().ConanUpdate;
        }
    }
}
