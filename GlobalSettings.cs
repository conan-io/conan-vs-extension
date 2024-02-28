using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace conan_vs_extension{
    public static class GlobalSettings
    {
        private static string _conanExecutablePath;

        public static string ConanExecutablePath
        {
            get
            {
                return _conanExecutablePath;
            }
            set
            {

                if (value == "")
                {
                    _conanExecutablePath = "conan";
                }
                else
                {
                    _conanExecutablePath = value;
                }
            }

        }
    }
}

