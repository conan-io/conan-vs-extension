namespace Conan.VisualStudio.Services
{
    public interface ISettingsService
    {
        /// <summary>Returns Conan executable path as defined in the project options.</summary>
        /// <returns>Executable path. May be <c>null</c> if Conan not found.</returns>
        string GetConanExecutablePath();
    }
}
