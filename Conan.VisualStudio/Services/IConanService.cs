using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Core;

namespace Conan.VisualStudio.Services
{
    public interface IConanService
    {
        Task IntegrateAsync(IVCProject vcProject);

        Task<bool> InstallAsync(IVCProject vcProject);
    }
}
