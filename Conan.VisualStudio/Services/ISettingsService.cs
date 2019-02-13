using Conan.VisualStudio.Core;

namespace Conan.VisualStudio.Services
{
    public interface ISettingsService
    {
        /// <summary>Returns Conan executable path as defined in the project options.</summary>
        /// <returns>Executable path. May be <c>null</c> if Conan not found.</returns>
        string GetConanExecutablePath();

        /// <summary>
        /// Try and load a project-level conan-vs-settings.json file
        /// </summary>
        /// <param name="project">Project</param>
        /// <returns>ConanSettings with overrides or null</returns>
        ConanSettings LoadSettingFile(ConanProject project);
    }
}
