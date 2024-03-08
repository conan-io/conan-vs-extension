using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace conan_vs_extension{
    public static class GlobalSettings
    {
        private static bool _conanExtensionEnabled;
        private static string _conanExecutablePath;

        public static bool ConanExtensionEnabled
        {
            get
            {
                return _conanExtensionEnabled;
            }
            set
            {
                _conanExtensionEnabled = value;
            }
        }

        public static string ConanExecutablePath
        {
            get
            {
                return _conanExecutablePath;
            }
            set
            {
                _conanExecutablePath = value;
            }
        }
    }
}

