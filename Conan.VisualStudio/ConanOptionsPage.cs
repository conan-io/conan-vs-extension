using System.ComponentModel;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    public class ConanOptionsPage : DialogPage
    {
        private string _conanExecutablePath;
        private bool? _installOnlyActiveConfiguration;
        private ConanGeneratorType? _conanGenerator;

        [Category("Conan")]
        [DisplayName("Conan executable")]
        [Description(@"Path to the Conan executable file, like C:\Python27\Scripts\conan.exe")]
        public string ConanExecutablePath
        {
            get => _conanExecutablePath ?? (_conanExecutablePath = ConanPathHelper.DetermineConanPathFromEnvironment());
            set => _conanExecutablePath = value;
        }

        [Category("Conan")]
        [DisplayName("Install only active configuration")]
        [Description(@"Install only active configuration, or all configurations")]
        public bool ConanInstallOnlyActiveConfiguration
        {
            get => _installOnlyActiveConfiguration ?? true;
            set => _installOnlyActiveConfiguration = value;
        }

        [Category("Conan")]
        [DisplayName("Generator")]
        [Description(@"Conan generator to use")]
        public ConanGeneratorType ConanGenerator
        {
            get => _conanGenerator ?? ConanGeneratorType.visual_studio_multi;
            set => _conanGenerator = value;
        }
    }
}
