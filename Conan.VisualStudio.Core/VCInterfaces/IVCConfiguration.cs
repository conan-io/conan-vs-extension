using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core.VCInterfaces
{
    public interface IVCConfiguration
    {
        string ProjectDirectory { get; }

        string ProjectName { get; }

        string Name { get; }

        string ConfigurationName { get; }

        string PlatformName { get; }

        string RuntimeLibrary { get; }

        string Toolset { get; }

        string Evaluate(string value);

        bool AddPropertySheet(string sheet);

        void CollectIntelliSenseInfo();

        List<IVCPropertySheet> PropertySheets { get; }

        string AdditionalDependencies { get; set; }
    }
}
