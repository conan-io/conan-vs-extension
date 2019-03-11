using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.IO;

namespace Conan.VisualStudio.Services
{
    internal class VisualStudioSettingsService : ISettingsService
    {
        private readonly Package _package;

        public VisualStudioSettingsService(Package package)
        {
            _package = package;
        }

        private ConanOptionsPage GetConanPage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Believe it or not, it's the recommended way of retrieving the option value per
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/creating-an-options-page#accessing-options
            return (ConanOptionsPage)_package.GetDialogPage(typeof(ConanOptionsPage));
        }

        public string GetConanExecutablePath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetConanPage().ConanExecutablePath;
        }

        public bool GetConanInstallOnlyActiveConfiguration()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetConanPage().ConanInstallOnlyActiveConfiguration;
        }

        /// <summary>
        /// Try and load a project-level conan-vs-settings.json file
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns>ConanSettings with overrides or null</returns>
        public ConanSettings LoadSettingFile(ConanProject project)
        {
            var projectDir = Path.GetDirectoryName(project.Path);
            var settingFileName = "conan-vs-settings.json";
            var settingPath = Path.Combine(projectDir, settingFileName);

            if (!File.Exists(settingPath)) return null;

            var conanSettings = JsonConvert.DeserializeObject<ConanSettings>(File.ReadAllText(settingPath));
            return conanSettings as ConanSettings;
        }
    }
}
