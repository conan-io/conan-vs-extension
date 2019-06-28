using Conan.VisualStudio.Core;

namespace Conan.VisualStudio.Services
{
    public interface ISettingsService
    {
        /// <summary>Returns Conan executable path as defined in the project options.</summary>
        /// <returns>Executable path. May be <c>null</c> if Conan not found.</returns>
        string GetConanExecutablePath();

        /// <summary>Returns True if install only active configuration, as defined in the project options.</summary>
        /// <returns>Boolean flag describing conan installation mode</returns>
        bool GetConanInstallOnlyActiveConfiguration();

        /// <summary>
        /// returns default conan generator, either visual_studio, or visual_studio_multi
        /// </summary>
        /// <returns>value of default conan generator type</returns>
        ConanGeneratorType GetConanGenerator();

        /// <summary>Returns True if install conan dependencies automatically, on solution load.</summary>
        /// <returns>Boolean flag describing conan installation mode</returns>
        bool GetConanInstallAutomatically();

        ConanBuildType GetConanBuild();
        bool GetConanUpdate();
    }
}
