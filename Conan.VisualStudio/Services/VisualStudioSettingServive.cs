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

        public string GetConanExecutablePath()
        {
            // Believe it or not, it's the recommended way of retrieving the option value per
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/creating-an-options-page#accessing-options
            var page = (ConanOptionsPage)_package.GetDialogPage(typeof(ConanOptionsPage));
            return page.ConanExecutablePath;
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
