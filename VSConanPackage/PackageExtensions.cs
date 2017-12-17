using Microsoft.VisualStudio.Shell;

namespace VSConanPackage
{
    internal static class PackageExtensions
    {
        /// <summary>Returns Conan executable path as defined in the project options.</summary>
        /// <param name="package">Current Visual Studio package (used to access the settings).</param>
        /// <returns>Executable path. May be <c>null</c> if Conan not found.</returns>
        public static string GetConanExecutablePath(this Package package)
        {
            // Believe it or not, it's the recommended way of retrieving the option value per
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/creating-an-options-page#accessing-options
            var page = (ConanOptionsPage)package.GetDialogPage(typeof(ConanOptionsPage));
            return page.ConanExecutablePath;
        }
    }
}
