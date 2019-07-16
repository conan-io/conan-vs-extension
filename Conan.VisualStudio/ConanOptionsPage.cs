using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.Shell;

namespace Conan.VisualStudio
{
    public class ConanOptionsPage : DialogPage
    {
        private string _conanExecutablePath;
        private string _conanInstallationPath;
        private bool? _conanInstallOnlyActiveConfiguration;
        private ConanGeneratorType? _conanGenerator;
        private bool? _conanInstallAutomatically;
        private ConanBuildType? _conanBuild;
        private bool? _conanUpdate;
        private static bool ValidateConanExecutable(string exe)
        {
            if (exe == null || exe == "")
                return true;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                var process = Process.Start(startInfo);

                process.WaitForExit();

                if (0 != process.ExitCode)
                    MessageBox.Show($"invalid conan executable {exe}: conan --version failed with error {process.ExitCode}",
                        "invalid conan executable", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return 0 == process.ExitCode;
            }
            catch (Win32Exception e)
            {
                MessageBox.Show($"invalid conan executable {exe}: {e.Message}", "invalid conan executable",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        [Category("Conan")]
        [DisplayName("Conan executable")]
        [Description(@"Path to the Conan executable file, like C:\Python27\Scripts\conan.exe")]
        public string ConanExecutablePath
        {
            get => _conanExecutablePath ?? (_conanExecutablePath = ConanPathHelper.DetermineConanPathFromEnvironment());
            set => _conanExecutablePath = ValidateConanExecutable(value) ? value : _conanExecutablePath;
        }

        [Category("Conan")]
        [DisplayName("Conan installation directory")]
        [Description(@"Path to the conan installation directory, may use macro like $(OutDir) or $(ProjectDir). Absolute or relative to the project directory.")]
        public string ConanInstallationPath
        {
            get => _conanInstallationPath ?? (_conanInstallationPath = "$(OutDir).conan");
            set => _conanInstallationPath = value;
        }

        [Category("Conan")]
        [DisplayName("Install only active configuration")]
        [Description(@"Install only active configuration, or all configurations")]
        public bool ConanInstallOnlyActiveConfiguration
        {
            get => _conanInstallOnlyActiveConfiguration ?? true;
            set => _conanInstallOnlyActiveConfiguration = value;
        }

        [Category("Conan")]
        [DisplayName("Generator")]
        [Description(@"Conan generator to use")]
        public ConanGeneratorType ConanGenerator
        {
            get => _conanGenerator ?? ConanGeneratorType.visual_studio;
            set => _conanGenerator = value;
        }

        [Category("Conan")]
        [DisplayName("Install conan dependencies automatically")]
        [Description(@"Install conan dependencies automatically on solution load")]
        public bool ConanInstallAutomatically
        {
            get => _conanInstallAutomatically ?? true;
            set => _conanInstallAutomatically = value;
        }

        [Category("Conan")]
        [DisplayName("Build policy")]
        [Description(@"--build argument (missing, outdated, always or none)")]
        public ConanBuildType ConanBuild
        {
            get => _conanBuild ?? ConanBuildType.missing;
            set => _conanBuild = value;
        }

        [Category("Conan")]
        [DisplayName("Update policy")]
        [Description(@"Check updates exist from upstream remotes")]
        public bool ConanUpdate
        {
            get => _conanUpdate ?? true;
            set => _conanUpdate = value;
        }
    }
}
