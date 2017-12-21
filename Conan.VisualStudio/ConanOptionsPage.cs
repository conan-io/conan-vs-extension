using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    public class ConanOptionsPage : DialogPage
    {
        private string _conanExecutablePath;

        [Category("Conan")]
        [DisplayName("Conan executable")]
        [Description(@"Path to the Conan executable file, like C:\Python27\Scripts\conan.exe")]
        public string ConanExecutablePath
        {
            get => _conanExecutablePath ?? (_conanExecutablePath = ConanPathHelper.DetermineConanPathFromEnvironment());
            set => _conanExecutablePath = value;
        }
    }
}
