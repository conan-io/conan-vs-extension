using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.Services
{
    public interface IVcProjectService
    {
        VCProject GetActiveProject();
        Task<ConanProject> ExtractConanProject(VCProject vcProject);
        Task AddPropsImport(string projectPath, string propFilePath);
    }
}
