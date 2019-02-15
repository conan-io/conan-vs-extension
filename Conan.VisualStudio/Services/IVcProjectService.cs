using System.Threading.Tasks;
using Conan.VisualStudio.Core;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.Services
{
    public interface IVcProjectService
    {
        VCProject GetActiveProject();
        Task<ConanProject> ExtractConanProjectAsync(VCProject vcProject);
        Task AddPropsImportAsync(string projectPath, string propFilePath);
        void UnloadProject(VCProject project);
        void ReloadProject(VCProject project);
    }
}
