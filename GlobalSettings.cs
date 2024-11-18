using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace conan_vs_extension{
    public static class GlobalSettings
    {
        private static string _conanExecutablePath = string.Empty;

        public static string ConanExecutablePath
        {
            get
            {
                return _conanExecutablePath;
            }
            set
            {
                _conanExecutablePath = value.Trim();
            }

        }
    }
}

