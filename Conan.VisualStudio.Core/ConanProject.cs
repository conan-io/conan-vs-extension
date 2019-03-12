using System.Collections.Generic;

namespace Conan.VisualStudio.Core
{
    public class ConanProject
    {
        public string Path { get; set; }

        public List<ConanConfiguration> Configurations { get; } = new List<ConanConfiguration>();
    }
}
