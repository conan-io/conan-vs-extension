using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core
{
    public interface IVCProject
    {
        string ProjectDirectory { get; }

        List<IVCConfiguration> Configurations { get; }

        IVCConfiguration ActiveConfiguration { get; }
    }
}
