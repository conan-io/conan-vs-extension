using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conan.VisualStudio.Core.VCInterfaces
{
    public interface IVCProject
    {
        string ProjectDirectory { get; }

        string FullPath { get; }

        string Guid { get; }

        bool Saved { get; }

        List<IVCConfiguration> Configurations { get; }

        IVCConfiguration ActiveConfiguration { get; }

        void Save();
    }
}
