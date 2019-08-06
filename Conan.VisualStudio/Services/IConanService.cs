using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.Services
{
    public interface IConanService
    {
        Task IntegrateAsync(VCProject vcProject);

        Task<bool> InstallAsync(VCProject vcProject, string conanPath);
    }
}
